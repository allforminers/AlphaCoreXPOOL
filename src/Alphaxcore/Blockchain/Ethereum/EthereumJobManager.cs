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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Alphaxcore.Blockchain.Bitcoin;
using Alphaxcore.Blockchain.Ethereum.Configuration;
using Alphaxcore.Blockchain.Ethereum.DaemonResponses;
using Alphaxcore.Configuration;
using Alphaxcore.Crypto.Hashing.Ethash;
using Alphaxcore.DaemonInterface;
using Alphaxcore.Extensions;
using Alphaxcore.JsonRpc;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications.Messages;
using Alphaxcore.Stratum;
using Alphaxcore.Time;
using Alphaxcore.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Block = Alphaxcore.Blockchain.Ethereum.DaemonResponses.Block;
using Contract = Alphaxcore.Contracts.Contract;
using EC = Alphaxcore.Blockchain.Ethereum.EthCommands;

namespace Alphaxcore.Blockchain.Ethereum
{
    public class EthereumJobManager : JobManagerBase<EthereumJob>
    {
        public EthereumJobManager(
            IComponentContext ctx,
            IMasterClock clock,
            IMessageBus messageBus,
            JsonSerializerSettings serializerSettings) :
            base(ctx, messageBus)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.clock = clock;

            serializer = new JsonSerializer
            {
                ContractResolver = serializerSettings.ContractResolver
            };
        }

        private DaemonEndpointConfig[] daemonEndpoints;
        private DaemonClient daemon;
        private EthereumNetworkType networkType;
        private ParityChainType chainType;
        private bool isParity = true;
        private EthashFull ethash;
        private readonly IMasterClock clock;
        private readonly EthereumExtraNonceProvider extraNonceProvider = new EthereumExtraNonceProvider();

        private const int MaxBlockBacklog = 3;
        protected readonly Dictionary<string, EthereumJob> validJobs = new Dictionary<string, EthereumJob>();
        private EthereumPoolConfigExtra extraPoolConfig;
        private readonly JsonSerializer serializer;

        protected async Task<bool> UpdateJobAsync()
        {
            logger.LogInvoke();

            try
            {
                return UpdateJob(await GetBlockTemplateAsync());
            }

            catch(Exception ex)
            {
                logger.Error(ex, () => $"Error during {nameof(UpdateJobAsync)}");
            }

            return false;
        }

        protected bool UpdateJob(EthereumBlockTemplate blockTemplate)
        {
            logger.LogInvoke();

            try
            {
                // may happen if daemon is currently not connected to peers
                if(blockTemplate == null || blockTemplate.Header?.Length == 0)
                    return false;

                // logger.Info(() => $"Blocktemplate {blockTemplate.Height}-{blockTemplate.Header}");

                var job = currentJob;
                var isNew = currentJob == null ||
                    job.BlockTemplate.Height < blockTemplate.Height ||
                    job.BlockTemplate.Header != blockTemplate.Header;

                if(isNew)
                {
                    messageBus.NotifyChainHeight(poolConfig.Id, blockTemplate.Height, poolConfig.Template);

                    var jobId = NextJobId("x8");

                    // update template
                    job = new EthereumJob(jobId, blockTemplate, logger);

                    lock(jobLock)
                    {
                        // add jobs
                        validJobs[jobId] = job;

                        // remove old ones
                        var obsoleteKeys = validJobs.Keys
                            .Where(key => validJobs[key].BlockTemplate.Height < job.BlockTemplate.Height - MaxBlockBacklog).ToArray();

                        foreach(var key in obsoleteKeys)
                            validJobs.Remove(key);
                    }

                    currentJob = job;

                    // update stats
                    BlockchainStats.LastNetworkBlockTime = clock.Now;
                    BlockchainStats.BlockHeight = job.BlockTemplate.Height;
                    BlockchainStats.NetworkDifficulty = job.BlockTemplate.Difficulty;
                    BlockchainStats.NextNetworkTarget = job.BlockTemplate.Target;
                    BlockchainStats.NextNetworkBits = "";
                }

                return isNew;
            }

            catch(Exception ex)
            {
                logger.Error(ex, () => $"Error during {nameof(UpdateJob)}");
            }

            return false;
        }

        private Task<EthereumBlockTemplate> GetBlockTemplateAsync()
        {
            if(isParity)
                return GetBlockTemplateParityAsync();

            return GetBlockTemplateGethAsync();
        }

        private async Task<EthereumBlockTemplate> GetBlockTemplateParityAsync()
        {
            logger.LogInvoke();

            var response = await daemon.ExecuteCmdAnyAsync<JToken>(logger, EC.GetWork);

            if(response.Error != null)
            {
                logger.Warn(() => $"Error(s) refreshing blocktemplate: {response.Error})");
                return null;
            }

            if(response.Response == null)
            {
                logger.Warn(() => $"Error(s) refreshing blocktemplate: {EC.GetWork} returned null response");
                return null;
            }

            // extract results
            var work = response.Response.ToObject<string[]>();
            var result = AssembleBlockTemplate(work);

            return result;
        }

        private async Task<EthereumBlockTemplate> GetBlockTemplateGethAsync()
        {
            logger.LogInvoke();

            var commands = new[]
            {
                new DaemonCmd(EC.GetWork),
                new DaemonCmd(EC.GetBlockByNumber, new[] { (object) "latest", true })
            };

            var results = await daemon.ExecuteBatchAnyAsync(logger, commands);

            if(results.Any(x => x.Error != null))
            {
                logger.Warn(() => $"Error(s) refreshing blocktemplate: {results.First(x => x.Error != null).Error.Message}");
                return null;
            }

            // extract results
            var work = results[0].Response.ToObject<string[]>();
            var block = results[1].Response.ToObject<Block>();

            // append blockheight (parity returns this as 4th element in the getWork response, geth does not)
            if(work.Length < 4)
            {
                var currentHeight = block.Height.Value;
                work = work.Concat(new[] { (currentHeight + 1).ToStringHexWithPrefix() }).ToArray();
            }

            var result = AssembleBlockTemplate(work);
            return result;
        }

        private EthereumBlockTemplate AssembleBlockTemplate(string[] work)
        {
            if(work.Length < 4)
            {
                logger.Error(() => $"Error(s) refreshing blocktemplate: getWork did not return blockheight. Are you really connected to a Parity daemon?");
                return null;
            }

            // extract values
            var height = work[3].IntegralFromHex<ulong>();
            var targetString = work[2];
            var target = BigInteger.Parse(targetString.Substring(2), NumberStyles.HexNumber);

            var result = new EthereumBlockTemplate
            {
                Header = work[0],
                Seed = work[1],
                Target = targetString,
                Difficulty = (ulong) BigInteger.Divide(EthereumConstants.BigMaxValue, target),
                Height = height,
            };

            return result;
        }

        private async Task ShowDaemonSyncProgressAsync()
        {
            var responses = await daemon.ExecuteCmdAllAsync<object>(logger, EC.GetSyncState);
            var firstValidResponse = responses.FirstOrDefault(x => x.Error == null && x.Response != null)?.Response;

            if(firstValidResponse != null)
            {
                // eth_syncing returns false if not synching
                if(firstValidResponse is bool)
                    return;

                var syncStates = responses.Where(x => x.Error == null && x.Response != null && firstValidResponse is JObject)
                    .Select(x => ((JObject) x.Response).ToObject<SyncState>())
                    .ToArray();

                if(syncStates.Any())
                {
                    // get peer count
                    var response = await daemon.ExecuteCmdAllAsync<string>(logger, EC.GetPeerCount);
                    var validResponses = response.Where(x => x.Error == null && x.Response != null).ToArray();
                    var peerCount = validResponses.Any() ? validResponses.Max(x => x.Response.IntegralFromHex<uint>()) : 0;

                    if(syncStates.Any(x => x.WarpChunksAmount != 0))
                    {
                        var warpChunkAmount = syncStates.Min(x => x.WarpChunksAmount);
                        var warpChunkProcessed = syncStates.Max(x => x.WarpChunksProcessed);
                        var percent = (double) warpChunkProcessed / warpChunkAmount * 100;

                        logger.Info(() => $"Daemons have downloaded {percent:0.00}% of warp-chunks from {peerCount} peers");
                    }

                    else if(syncStates.Any(x => x.HighestBlock != 0))
                    {
                        var lowestHeight = syncStates.Min(x => x.CurrentBlock);
                        var totalBlocks = syncStates.Max(x => x.HighestBlock);
                        var percent = (double) lowestHeight / totalBlocks * 100;

                        logger.Info(() => $"Daemons have downloaded {percent:0.00}% of blockchain from {peerCount} peers");
                    }
                }
            }
        }

        private async Task UpdateNetworkStatsAsync()
        {
            logger.LogInvoke();

            try
            {
                var results = await daemon.ExecuteBatchAnyAsync(logger, 
                    new DaemonCmd(EC.GetPeerCount),
                    new DaemonCmd(EC.GetBlockByNumber, new[] { (object) "latest", true })
                );
                
                if(results.Any(x => x.Error != null))
                {
                    var errors = results.Where(x => x.Error != null)
                        .ToArray();

                    if(errors.Any())
                        logger.Warn(() => $"Error(s) refreshing network stats: {string.Join(", ", errors.Select(y => y.Error.Message))})");
                }

                var peerCount = results[0].Response.ToObject<string>().IntegralFromHex<int>();
                var latestBlockInfo = results[1].Response.ToObject<Block>();
                var latestBlockHeight = latestBlockInfo.Height.Value;
                var latestBlockTimestamp = latestBlockInfo.Timestamp;
                var latestBlockDifficulty = latestBlockInfo.Difficulty.IntegralFromHex<ulong>();

                ulong sampleBlockCount = 50;
                var sampleBlockNumber = latestBlockHeight - sampleBlockCount;
                var sampleBlockResults = await daemon.ExecuteCmdAllAsync<DaemonResponses.Block>(logger, EC.GetBlockByNumber, new[] { (object) sampleBlockNumber.ToStringHexWithPrefix(), true });
                var sampleBlockHeight = sampleBlockResults.First(x => x.Error == null && x.Response?.Height != null).Response.Height.Value;
                var sampleBlockTimestamp = sampleBlockResults.First(x => x.Error == null && x.Response?.Height != null).Response.Timestamp;
                                 
                ulong BlockTimeFrame = latestBlockTimestamp - sampleBlockTimestamp;
                ulong blockAvgTime = BlockTimeFrame / sampleBlockCount;

                BlockchainStats.ConnectedPeers = peerCount;
                BlockchainStats.NetworkHashrate = blockAvgTime > 0 ? (double) latestBlockDifficulty / blockAvgTime : 0;
                BlockchainStats.CurrentTime = clock.Now;
            }

            catch(Exception e)
            {
                logger.Error(e);
            }
        }

        private async Task<bool> SubmitBlockAsync(Share share, string fullNonceHex, string headerHash, string mixHash)
        {
            // submit work
            var response = await daemon.ExecuteCmdAnyAsync<object>(logger, EC.SubmitWork, new[]
            {
                fullNonceHex,
                headerHash,
                mixHash
            });

            if(response.Error != null || (bool?) response.Response == false)
            {
                var error = response.Error?.Message ?? response?.Response?.ToString();

                logger.Warn(() => $"Block {share.BlockHeight} submission failed with: {error}");
                messageBus.SendMessage(new AdminNotification("Block submission failed", $"Pool {poolConfig.Id} {(!string.IsNullOrEmpty(share.Source) ? $"[{share.Source.ToUpper()}] " : string.Empty)}failed to submit block {share.BlockHeight}: {error}"));

                return false;
            }

            return true;
        }

        private object[] GetJobParamsForStratum(bool isNew)
        {
            var job = currentJob;

            if(job != null)
            {
                return new object[]
                {
                    job.Id,
                    job.BlockTemplate.Seed.StripHexPrefix(),
                    job.BlockTemplate.Header.StripHexPrefix(),
                    isNew
                };
            }

            return new object[0];
        }

        private JsonRpcRequest DeserializeRequest(byte[] data)
        {
            using(var stream = new MemoryStream(data))
            {
                using(var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    using(var jreader = new JsonTextReader(reader))
                    {
                        return serializer.Deserialize<JsonRpcRequest>(jreader);
                    }
                }
            }
        }

        #region API-Surface

        public IObservable<object> Jobs { get; private set; }

        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {
            extraPoolConfig = poolConfig.Extra.SafeExtensionDataAs<EthereumPoolConfigExtra>();

            // extract standard daemon endpoints
            daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            base.Configure(poolConfig, clusterConfig);

            if(poolConfig.EnableInternalStratum == true)
            {
                // ensure dag location is configured
                var dagDir = !string.IsNullOrEmpty(extraPoolConfig?.DagDir) ?
                    Environment.ExpandEnvironmentVariables(extraPoolConfig.DagDir) :
                    Dag.GetDefaultDagDirectory();

                // create it if necessary
                Directory.CreateDirectory(dagDir);

                // setup ethash
                ethash = new EthashFull(3, dagDir);
            }
        }

        public bool ValidateAddress(string address)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(address), $"{nameof(address)} must not be empty");

            if(EthereumConstants.ZeroHashPattern.IsMatch(address) ||
                !EthereumConstants.ValidAddressPattern.IsMatch(address))
                return false;

            return true;
        }

        public void PrepareWorker(StratumClient client)
        {
            var context = client.ContextAs<EthereumWorkerContext>();
            context.ExtraNonce1 = extraNonceProvider.Next();
        }

        public async ValueTask<Share> SubmitShareAsync(StratumClient worker, string[] request, CancellationToken ct)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.RequiresNonNull(request, nameof(request));

            logger.LogInvoke(new[] { worker.ConnectionId });
            var context = worker.ContextAs<EthereumWorkerContext>();

            EthereumJob job;
            string miner, jobId, nonce = string.Empty;
            if(context.IsNiceHashClient)
            {
                jobId = request[1];
                nonce = request[2];
                miner = request[0];

                lock(jobLock)
                {
                    var jobResult = validJobs.Where(x => x.Value.Id == jobId).FirstOrDefault();
                    if(jobResult.Value == null)
                        throw new StratumException(StratumError.MinusOne, "stale share");
                    job = jobResult.Value;
                }
            }

            else
            {
                jobId = request[1];
                nonce = request[0];

                lock(jobLock)
                {
                    var jobResult = validJobs.Where(x => x.Value.BlockTemplate.Header == jobId).FirstOrDefault();
                    if(jobResult.Value == null)
                        throw new StratumException(StratumError.MinusOne, "stale share");
                    job = jobResult.Value;
                }
            }

            // validate & process
            var (share, fullNonceHex, headerHash, mixHash) = await job.ProcessShareAsync(worker, nonce, ethash, ct);

            // enrich share with common data
            share.PoolId = poolConfig.Id;
            share.NetworkDifficulty = BlockchainStats.NetworkDifficulty;
            share.Source = clusterConfig.ClusterName;
            share.Created = clock.Now;

            // if block candidate, submit & check if accepted by network
            if(share.IsBlockCandidate)
            {
                logger.Info(() => $"Submitting block {share.BlockHeight}");

                share.IsBlockCandidate = await SubmitBlockAsync(share, fullNonceHex, headerHash, mixHash);

                if(share.IsBlockCandidate)
                {
                    logger.Info(() => $"Daemon accepted block {share.BlockHeight} submitted by {context.Miner}");
                }
            }

            return share;
        }

        public BlockchainStats BlockchainStats { get; } = new BlockchainStats();

        #endregion // API-Surface

        #region Overrides

        protected override void ConfigureDaemons()
        {
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            daemon = new DaemonClient(jsonSerializerSettings, messageBus, clusterConfig.ClusterName ?? poolConfig.PoolName, poolConfig.Id);
            daemon.Configure(daemonEndpoints);
        }

        protected override async Task<bool> AreDaemonsHealthyAsync()
        {
            var responses = await daemon.ExecuteCmdAllAsync<Block>(logger, EC.GetBlockByNumber, new[] { (object) "latest", true });

            if(responses.Where(x => x.Error?.InnerException?.GetType() == typeof(DaemonClientException))
                .Select(x => (DaemonClientException) x.Error.InnerException)
                .Any(x => x.Code == HttpStatusCode.Unauthorized))
                logger.ThrowLogPoolStartupException($"Daemon reports invalid credentials");

            return responses.All(x => x.Error == null);
        }

        protected override async Task<bool> AreDaemonsConnectedAsync()
        {
            var response = await daemon.ExecuteCmdAnyAsync<string>(logger, EC.GetPeerCount);

            return response.Error == null && response.Response.IntegralFromHex<uint>() > 0;
        }

        protected override async Task EnsureDaemonsSynchedAsync(CancellationToken ct)
        {
            var syncPendingNotificationShown = false;

            while(true)
            {
                var responses = await daemon.ExecuteCmdAllAsync<object>(logger, EC.GetSyncState);

                var isSynched = responses.All(x => x.Error == null &&
                    x.Response is bool && (bool) x.Response == false);

                if(isSynched)
                {
                    logger.Info(() => $"All daemons synched with blockchain");
                    break;
                }

                if(!syncPendingNotificationShown)
                {
                    logger.Info(() => $"Daemons still syncing with network. Manager will be started once synced");
                    syncPendingNotificationShown = true;
                }

                await ShowDaemonSyncProgressAsync();

                // delay retry by 5s
                await Task.Delay(5000, ct);
            }
        }

        protected override async Task PostStartInitAsync(CancellationToken ct)
        {
            var commands = new[]
            {
                new DaemonCmd(EC.GetNetVersion),
                new DaemonCmd(EC.GetAccounts),
                new DaemonCmd(EC.GetCoinbase),
                new DaemonCmd(EC.ParityChain),
            };

            var results = await daemon.ExecuteBatchAnyAsync(logger, commands);

            if(results.Any(x => x.Error != null))
            {
                if(results[3].Error != null)
                    isParity = false;

                var errors = results.Take(3).Where(x => x.Error != null)
                    .ToArray();

                if(errors.Any())
                    logger.ThrowLogPoolStartupException($"Init RPC failed: {string.Join(", ", errors.Select(y => y.Error.Message))}");
            }

            // extract results
            var netVersion = results[0].Response.ToObject<string>();
            var accounts = results[1].Response.ToObject<string[]>();
            var coinbase = results[2].Response.ToObject<string>();
            var parityChain = isParity ?
                results[3].Response.ToObject<string>() :
                (extraPoolConfig?.ChainTypeOverride ?? "Mainnet");

            // ensure pool owns wallet
            //if (clusterConfig.PaymentProcessing?.Enabled == true && !accounts.Contains(poolConfig.Address) || coinbase != poolConfig.Address)
            //    logger.ThrowLogPoolStartupException($"Daemon does not own pool-address '{poolConfig.Address}'", LogCat);

            EthereumUtils.DetectNetworkAndChain(netVersion, parityChain, out networkType, out chainType);

            if(clusterConfig.PaymentProcessing?.Enabled == true && poolConfig.PaymentProcessing?.Enabled == true)
                ConfigureRewards();

            // update stats
            BlockchainStats.RewardType = "POW";
            BlockchainStats.NetworkType = $"{chainType}-{networkType}";

            await UpdateNetworkStatsAsync();

            // Periodically update network stats
            Observable.Interval(TimeSpan.FromSeconds(30))
                .Select(via => Observable.FromAsync(async () =>
                {
                    try
                    {
                        await UpdateNetworkStatsAsync();
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                }))
                .Concat()
                .Subscribe();

            if(poolConfig.EnableInternalStratum == true)
            {
                // make sure we have a current DAG
                while(true)
                {
                    var blockTemplate = await GetBlockTemplateAsync();

                    if(blockTemplate != null)
                    {
                        logger.Info(() => $"Loading current DAG ...");

                        await ethash.GetDagAsync(blockTemplate.Height, logger, ct);

                        logger.Info(() => $"Loaded current DAG");
                        break;
                    }

                    logger.Info(() => $"Waiting for first valid block template");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }

            await SetupJobUpdatesAsync();
        }

        private void ConfigureRewards()
        {
            if(networkType == EthereumNetworkType.Main &&
                chainType == ParityChainType.Mainnet &&
                DevDonation.Addresses.TryGetValue(poolConfig.Template.As<CoinTemplate>().Symbol, out var address))
            {
                poolConfig.RewardRecipients = poolConfig.RewardRecipients.Concat(new[]
                {
                    new RewardRecipient
                    {
                        Address = address,
                        Percentage = DevDonation.Percent,
                        Type = "dev"
                    }
                }).ToArray();
            }
        }

        protected virtual async Task SetupJobUpdatesAsync()
        {
            if(extraPoolConfig?.BtStream == null)
            {
                var enableStreaming = extraPoolConfig?.EnableDaemonWebsocketStreaming == true;

                if(enableStreaming && !poolConfig.Daemons.Any(x =>
                   x.Extra.SafeExtensionDataAs<EthereumDaemonEndpointConfigExtra>()?.PortWs.HasValue == true))
                {
                    logger.Warn(() => $"'{nameof(EthereumPoolConfigExtra.EnableDaemonWebsocketStreaming).ToLowerCamelCase()}' enabled but not a single daemon found with a configured websocket port ('{nameof(EthereumDaemonEndpointConfigExtra.PortWs).ToLowerCamelCase()}'). Falling back to polling.");
                    enableStreaming = false;
                }

                if(enableStreaming)
                {
                    // collect ports
                    var wsDaemons = poolConfig.Daemons
                        .Where(x => x.Extra.SafeExtensionDataAs<EthereumDaemonEndpointConfigExtra>()?.PortWs.HasValue == true)
                        .ToDictionary(x => x, x =>
                        {
                            var extra = x.Extra.SafeExtensionDataAs<EthereumDaemonEndpointConfigExtra>();

                            return (extra.PortWs.Value, extra.HttpPathWs, extra.SslWs);
                        });

                    logger.Info(() => $"Subscribing to WebSocket(s) {string.Join(", ", wsDaemons.Keys.Select(x => $"{(wsDaemons[x].SslWs ? "wss" : "ws")}://{x.Host}:{wsDaemons[x].Value}").Distinct())}");

                    if(isParity)
                    {
                        // stream work updates
                        var getWorkObs = daemon.WebsocketSubscribe(logger, wsDaemons, EC.ParitySubscribe, new[] { (object) EC.GetWork })
                            .Select(data =>
                            {
                                try
                                {
                                    var psp = DeserializeRequest(data).ParamsAs<PubSubParams<string[]>>();
                                    return psp?.Result;
                                }

                                catch(Exception ex)
                                {
                                    logger.Info(() => $"Error deserializing pending block: {ex.Message}");
                                }

                                return null;
                            });

                        Jobs = getWorkObs.Where(x => x != null)
                            .Select(AssembleBlockTemplate)
                            .Select(UpdateJob)
                            .Do(isNew =>
                            {
                                if(isNew)
                                    logger.Info(() => $"New work at height {currentJob.BlockTemplate.Height} and header {currentJob.BlockTemplate.Header} detected [{JobRefreshBy.WebSocket}]");
                            })
                            .Where(isNew => isNew)
                            .Select(_ => GetJobParamsForStratum(true))
                            .Publish()
                            .RefCount();
                    }

                    else
                    {
                        var wsSubscription = "newHeads";
                        var isRetry = false;
                    retry:

                        // stream work updates
                        var getWorkObs = daemon.WebsocketSubscribe(logger, wsDaemons, EC.Subscribe, new[] { (object) wsSubscription, new object() });

                        // test subscription
                        var subcriptionResponse = await getWorkObs
                            .Take(1)
                            .Select(x => JsonConvert.DeserializeObject<JsonRpcResponse<string>>(Encoding.UTF8.GetString(x)))
                            .ToTask();

                        if(subcriptionResponse.Error != null)
                        {
                            // older versions of geth only support subscriptions to "newBlocks"
                            if(!isRetry && subcriptionResponse.Error.Code == (int) BitcoinRPCErrorCode.RPC_METHOD_NOT_FOUND)
                            {
                                wsSubscription = "newBlocks";

                                isRetry = true;
                                goto retry;
                            }

                            logger.ThrowLogPoolStartupException($"Unable to subscribe to geth websocket '{wsSubscription}': {subcriptionResponse.Error.Message} [{subcriptionResponse.Error.Code}]");
                        }

                        Jobs = getWorkObs.Where(x => x != null)
                            .Select(_ => Observable.FromAsync(UpdateJobAsync))
                            .Concat()
                            .Do(isNew =>
                            {
                                if(isNew)
                                    logger.Info(() => $"New work at height {currentJob.BlockTemplate.Height} and header {currentJob.BlockTemplate.Header} detected [WS]");
                            })
                            .Where(isNew => isNew)
                            .Select(_ => GetJobParamsForStratum(true))
                            .Publish()
                            .RefCount();
                    }
                }

                else
                {
                    var pollingInterval = poolConfig.BlockRefreshInterval > 0 ? poolConfig.BlockRefreshInterval : 1000;

                    Jobs = Observable.Interval(TimeSpan.FromMilliseconds(pollingInterval))
                        .Select(_ => Observable.FromAsync(UpdateJobAsync))
                        .Concat()
                        .Do(isNew =>
                        {
                            if(isNew)
                                logger.Info(() => $"New work at height {currentJob.BlockTemplate.Height} and header {currentJob.BlockTemplate.Header} detected [{JobRefreshBy.Poll}]");
                        })
                        .Where(isNew => isNew)
                        .Select(_ => GetJobParamsForStratum(true))
                        .Publish()
                        .RefCount();
                }
            }

            else
            {
                var btStream = BtStreamSubscribe(extraPoolConfig.BtStream);

                Jobs = btStream.Where(x => x != null)
                    .Select(JsonConvert.DeserializeObject<string[]>)
                    .Select(AssembleBlockTemplate)
                    .Select(UpdateJob)
                    .Do(isNew =>
                    {
                        if(isNew)
                            logger.Info(() => $"New work at height {currentJob.BlockTemplate.Height} and header {currentJob.BlockTemplate.Header} detected [{JobRefreshBy.BlockTemplateStream}]");
                    })
                    .Where(isNew => isNew)
                    .Select(_ => GetJobParamsForStratum(true))
                    .Publish()
                    .RefCount();
            }
        }

        #endregion // Overrides
    }
}
