/*
Copyright 2017 - 2020 Coin Foundry (coinfoundry.org)
Copyright 2020 - 2021 AlphaX Projects (alphax.pro)
Authors: Oliver Weichhold (oliver@weichhold.com)
         Olaf Wasilewski (olaf.wasilewski@gmx.de)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Alphaxcore.Blockchain.Bitcoin;
using Alphaxcore.Blockchain.Bitcoin.DaemonResponses;
using Alphaxcore.Blockchain.Equihash.Custom.BitcoinGold;
using Alphaxcore.Blockchain.Equihash.Custom.VerusCoin;
using Alphaxcore.Blockchain.Equihash.Custom.Minexcoin;
using Alphaxcore.Blockchain.Equihash.DaemonResponses;
using Alphaxcore.Configuration;
using Alphaxcore.Contracts;
using Alphaxcore.Crypto.Hashing.Equihash;
using Alphaxcore.DaemonInterface;
using Alphaxcore.Extensions;
using Alphaxcore.JsonRpc;
using Alphaxcore.Messaging;
using Alphaxcore.Stratum;
using Alphaxcore.Time;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using NLog;

namespace Alphaxcore.Blockchain.Equihash
{
    public class EquihashJobManager : BitcoinJobManagerBase<EquihashJob>
    {
        public EquihashJobManager(
            IComponentContext ctx,
            IMasterClock clock,
            IMessageBus messageBus,
            IExtraNonceProvider extraNonceProvider) : base(ctx, clock, messageBus, extraNonceProvider)
        {
            getBlockTemplateParams = null;
        }

        private EquihashCoinTemplate coin;
        public EquihashCoinTemplate.EquihashNetworkParams ChainConfig { get; private set; }
        private EquihashSolver solver;

        protected override void PostChainIdentifyConfigure()
        {
            ChainConfig = coin.GetNetwork(network.NetworkType);
            solver = EquihashSolverFactory.GetSolver(ctx, ChainConfig.Solver);

            base.PostChainIdentifyConfigure();
        }

        private async Task<DaemonResponse<EquihashBlockTemplate>> GetBlockTemplateAsync()
        {
            logger.LogInvoke();

            var subsidyResponse = await daemon.ExecuteCmdAnyAsync<ZCashBlockSubsidy>(logger, BitcoinCommands.GetBlockSubsidy);

            var result = await daemon.ExecuteCmdAnyAsync<EquihashBlockTemplate>(logger,
                BitcoinCommands.GetBlockTemplate, extraPoolConfig?.GBTArgs ?? (object) getBlockTemplateParams);

            if(subsidyResponse.Error == null && result.Error == null && result.Response != null)
                result.Response.Subsidy = subsidyResponse.Response;
            else if(subsidyResponse.Error?.Code != (int) BitcoinRPCErrorCode.RPC_METHOD_NOT_FOUND)
                result.Error = new JsonRpcException(-1, $"{BitcoinCommands.GetBlockSubsidy} failed", null);

            return result;
        }

        private DaemonResponse<EquihashBlockTemplate> GetBlockTemplateFromJson(string json)
        {
            logger.LogInvoke();

            var result = JsonConvert.DeserializeObject<JsonRpcResponse>(json);

            return new DaemonResponse<EquihashBlockTemplate>
            {
                Response = result.ResultAs<EquihashBlockTemplate>(),
            };
        }

        protected override IDestination AddressToDestination(string address, BitcoinAddressType? addressType)
        {
            if(!coin.UsesZCashAddressFormat)
                return base.AddressToDestination(address, addressType);

            var decoded = Encoders.Base58.DecodeData(address);
            var hash = decoded.Skip(2).Take(20).ToArray();
            var result = new KeyId(hash);
            return result;
        }

        private EquihashJob CreateJob()
        {
            switch(coin.Symbol)
            {
                case "BTG":
                    return new BitcoinGoldJob();
                
                case "VRSC":
                    return new VerusCoinJob();

                case "MNX":
                    return new MinexcoinJob();
            }

            return new EquihashJob();
        }

        protected override async Task<(bool IsNew, bool Force)> UpdateJob(bool forceUpdate, string via = null, string json = null)
        {
            logger.LogInvoke();

            try
            {
                if(forceUpdate)
                    lastJobRebroadcast = clock.Now;

                var response = string.IsNullOrEmpty(json) ?
                    await GetBlockTemplateAsync() :
                    GetBlockTemplateFromJson(json);

                // may happen if daemon is currently not connected to peers
                if(response.Error != null)
                {
                    logger.Warn(() => $"Unable to update job. Daemon responded with: {response.Error.Message} Code {response.Error.Code}");
                    return (false, forceUpdate);
                }

                var blockTemplate = response.Response;
                var job = currentJob;

                var isNew = job == null ||
                    (blockTemplate != null &&
                        (job.BlockTemplate?.PreviousBlockhash != blockTemplate.PreviousBlockhash ||
                        blockTemplate.Height > job.BlockTemplate?.Height));

                if(isNew)
                    messageBus.NotifyChainHeight(poolConfig.Id, blockTemplate.Height, poolConfig.Template);

                if(isNew || forceUpdate)
                {
                    job = CreateJob();

                    job.Init(blockTemplate, NextJobId(),
                        poolConfig, clusterConfig, clock, poolAddressDestination, network, solver);

                    lock(jobLock)
                    {
                        validJobs.Insert(0, job);

                        // trim active jobs
                        while(validJobs.Count > maxActiveJobs)
                            validJobs.RemoveAt(validJobs.Count - 1);
                    }

                    if(isNew)
                    {
                        if(via != null)
                            logger.Info(() => $"Detected new block {blockTemplate.Height} [{via}]");
                        else
                            logger.Info(() => $"Detected new block {blockTemplate.Height}");

                        // update stats
                        BlockchainStats.LastNetworkBlockTime = clock.Now;
                        BlockchainStats.BlockHeight = blockTemplate.Height;
                        BlockchainStats.NetworkDifficulty = job.Difficulty;
                        BlockchainStats.NextNetworkTarget = blockTemplate.Target;
                        BlockchainStats.NextNetworkBits = blockTemplate.Bits;
                    }

                    currentJob = job;
                }

                return (isNew, forceUpdate);
            }

            catch(Exception ex)
            {
                logger.Error(ex, () => $"Error during {nameof(UpdateJob)}");
            }

            return (false, forceUpdate);
        }

        protected override object GetJobParamsForStratum(bool isNew)
        {
            var job = currentJob;
            return job?.GetJobParams(isNew);
        }

        #region API-Surface

        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {
            coin = poolConfig.Template.As<EquihashCoinTemplate>();

            base.Configure(poolConfig, clusterConfig);
        }

        public override async Task<bool> ValidateAddressAsync(string address, CancellationToken ct)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(address), $"{nameof(address)} must not be empty");

            // handle t-addr
            if(await base.ValidateAddressAsync(address, ct))
                return true;

            // handle z-addr
            var result = await daemon.ExecuteCmdAnyAsync<ValidateAddressResponse>(logger, ct,
                EquihashCommands.ZValidateAddress, new[] { address });

            return result.Response != null && result.Response.IsValid;
        }

        public override object[] GetSubscriberData(StratumClient worker)
        {
            Contract.RequiresNonNull(worker, nameof(worker));

            var context = worker.ContextAs<BitcoinWorkerContext>();

            // assign unique ExtraNonce1 to worker (miner)
            context.ExtraNonce1 = extraNonceProvider.Next();

            // setup response data
            var responseData = new object[]
            {
                context.ExtraNonce1
            };

            return responseData;
        }

        public override async ValueTask<Share> SubmitShareAsync(StratumClient worker, object submission,
            double stratumDifficultyBase, CancellationToken ct)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.RequiresNonNull(submission, nameof(submission));

            logger.LogInvoke(new[] { worker.ConnectionId });

            if(!(submission is object[] submitParams))
                throw new StratumException(StratumError.Other, "invalid params");

            var context = worker.ContextAs<BitcoinWorkerContext>();

            // extract params
            var workerValue = (submitParams[0] as string)?.Trim();
            var jobId = submitParams[1] as string;
            var nTime = submitParams[2] as string;
            var extraNonce2 = submitParams[3] as string;
            var solution = submitParams[4] as string;

            if(string.IsNullOrEmpty(workerValue))
                throw new StratumException(StratumError.Other, "missing or invalid workername");

            if(string.IsNullOrEmpty(solution))
                throw new StratumException(StratumError.Other, "missing or invalid solution");

            EquihashJob job;

            lock(jobLock)
            {
                job = validJobs.FirstOrDefault(x => x.JobId == jobId);
            }

            if(job == null)
                throw new StratumException(StratumError.JobNotFound, "job not found");

            // extract worker/miner/payoutid
            var split = workerValue.Split('.');
            var minerName = split[0];
            var workerName = split.Length > 1 ? split[1] : "0";

            // validate & process
            var (share, blockHex) = job.ProcessShare(worker, extraNonce2, nTime, solution);

            // if block candidate, submit & check if accepted by network
            if(share.IsBlockCandidate)
            {
                logger.Info(() => $"Submitting block {share.BlockHeight} [{share.BlockHash}]");

                var acceptResponse = await SubmitBlockAsync(share, blockHex);

                // is it still a block candidate?
                share.IsBlockCandidate = acceptResponse.Accepted;

                if(share.IsBlockCandidate)
                {
                    logger.Info(() => $"Daemon accepted block {share.BlockHeight} [{share.BlockHash}] submitted by {minerName}");

                    OnBlockFound();

                    // persist the coinbase transaction-hash to allow the payment processor
                    // to verify later on that the pool has received the reward for the block
                    share.TransactionConfirmationData = acceptResponse.CoinbaseTx;
                }

                else
                {
                    // clear fields that no longer apply
                    share.TransactionConfirmationData = null;
                }
            }

            // enrich share with common data
            share.PoolId = poolConfig.Id;
            share.IpAddress = worker.RemoteEndpoint.Address.ToString();
            share.Miner = minerName;
            share.Worker = workerName;
            share.UserAgent = context.UserAgent;
            share.Source = clusterConfig.ClusterName;
            share.NetworkDifficulty = job.Difficulty;
            share.Difficulty = share.Difficulty;
            share.Created = clock.Now;

            return share;
        }


        #endregion // API-Surface
    }
}
