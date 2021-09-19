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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using Alphaxcore.Blockchain.Ethereum.Configuration;
using Alphaxcore.Blockchain.Ethereum.DaemonRequests;
using Alphaxcore.Blockchain.Ethereum.DaemonResponses;
using Alphaxcore.Configuration;
using Alphaxcore.DaemonInterface;
using Alphaxcore.Extensions;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications;
using Alphaxcore.Notifications.Messages;
using Alphaxcore.Payments;
using Alphaxcore.Persistence;
using Alphaxcore.Persistence.Model;
using Alphaxcore.Persistence.Repositories;
using Alphaxcore.Time;
using Alphaxcore.Util;
using Newtonsoft.Json;
using Block = Alphaxcore.Persistence.Model.Block;
using Contract = Alphaxcore.Contracts.Contract;
using EC = Alphaxcore.Blockchain.Ethereum.EthCommands;

namespace Alphaxcore.Blockchain.Ethereum
{
    [CoinFamily(CoinFamily.Ethereum)]
    public class EthereumPayoutHandler : PayoutHandlerBase,
        IPayoutHandler
    {
        public EthereumPayoutHandler(
            IComponentContext ctx,
            IConnectionFactory cf,
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            IMasterClock clock,
            IMessageBus messageBus) :
            base(cf, mapper, shareRepo, blockRepo, balanceRepo, paymentRepo, clock, messageBus)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(balanceRepo, nameof(balanceRepo));
            Contract.RequiresNonNull(paymentRepo, nameof(paymentRepo));

            this.ctx = ctx;
        }

        private readonly IComponentContext ctx;
        private DaemonClient daemon;
        private EthereumNetworkType networkType;
        private ParityChainType chainType;
        private const int BlockSearchOffset = 7;
        private EthereumPoolConfigExtra extraPoolConfig;
        private EthereumPoolPaymentProcessingConfigExtra extraConfig;
        private bool isParity = true;

        protected override string LogCategory => "Ethereum Payout Handler";

        #region IPayoutHandler

        public async Task ConfigureAsync(ClusterConfig clusterConfig, PoolConfig poolConfig)
        {
            this.poolConfig = poolConfig;
            this.clusterConfig = clusterConfig;
            extraPoolConfig = poolConfig.Extra.SafeExtensionDataAs<EthereumPoolConfigExtra>();
            extraConfig = poolConfig.PaymentProcessing.Extra.SafeExtensionDataAs<EthereumPoolPaymentProcessingConfigExtra>();

            logger = LogUtil.GetPoolScopedLogger(typeof(EthereumPayoutHandler), poolConfig);

            // configure standard daemon
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            var daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            daemon = new DaemonClient(jsonSerializerSettings, messageBus, clusterConfig.ClusterName ?? poolConfig.PoolName, poolConfig.Id);
            daemon.Configure(daemonEndpoints);

            await DetectChainAsync();
        }

        public async Task<Block[]> ClassifyBlocksAsync(Block[] blocks)
        {
            Contract.RequiresNonNull(poolConfig, nameof(poolConfig));
            Contract.RequiresNonNull(blocks, nameof(blocks));

            var coin = poolConfig.Template.As<EthereumCoinTemplate>();
            var pageSize = 100;
            var pageCount = (int) Math.Ceiling(blocks.Length / (double) pageSize);
            var blockCache = new Dictionary<long, DaemonResponses.Block>();
            var result = new List<Block>();

            for(var i = 0; i < pageCount; i++)
            {
                // get a page full of blocks
                var page = blocks
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToArray();

                // get latest block
                var latestBlockResponses = await daemon.ExecuteCmdAllAsync<DaemonResponses.Block>(logger, EC.GetBlockByNumber, new[] { (object) "latest", true });
                var latestBlockHeight = latestBlockResponses.First(x => x.Error == null && x.Response?.Height != null).Response.Height.Value;

                // execute batch
                var blockInfos = await FetchBlocks(blockCache, page.Select(block => (long) block.BlockHeight).ToArray());

                for(var j = 0; j < blockInfos.Length; j++)
                {
                    var blockInfo = blockInfos[j];
                    var block = page[j];

                    // extract confirmation data from stored block
                    var mixHash = block.TransactionConfirmationData.Split(":").First();
                    var nonce = block.TransactionConfirmationData.Split(":").Last();

                    // update progress
                    block.ConfirmationProgress = Math.Min(1.0d, (double) (latestBlockHeight - block.BlockHeight) / EthereumConstants.MinConfimations);
                    result.Add(block);

                    messageBus.NotifyBlockConfirmationProgress(poolConfig.Id, block, coin);

                    // is it block mined by us?
                    if(string.Equals(blockInfo.Miner, poolConfig.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        // mature?
                        if(latestBlockHeight - block.BlockHeight >= EthereumConstants.MinConfimations)
                        {
                            block.Status = BlockStatus.Confirmed;
                            block.ConfirmationProgress = 1;
                            block.BlockHeight = (ulong) blockInfo.Height;
                            block.Reward = GetBaseBlockReward(chainType, block.BlockHeight); // base reward
                            block.Type = "block";

                            if(extraConfig?.KeepUncles == false)
                                block.Reward += blockInfo.Uncles.Length * (block.Reward / 32); // uncle rewards

                            if(extraConfig?.KeepTransactionFees == false && blockInfo.Transactions?.Length > 0)
                                block.Reward += await GetTxRewardAsync(blockInfo); // tx fees

                            logger.Info(() => $"[{LogCategory}] Unlocked block {block.BlockHeight} worth {FormatAmount(block.Reward)}");

                            messageBus.NotifyBlockUnlocked(poolConfig.Id, block, coin);
                        }

                        continue;
                    }

                    // search for a block containing our block as an uncle by checking N blocks in either direction
                    var heightMin = block.BlockHeight - BlockSearchOffset;
                    var heightMax = Math.Min(block.BlockHeight + BlockSearchOffset, latestBlockHeight);
                    var range = new List<long>();

                    for(var k = heightMin; k < heightMax; k++)
                        range.Add((long) k);

                    // execute batch
                    var blockInfo2s = await FetchBlocks(blockCache, range.ToArray());

                    foreach(var blockInfo2 in blockInfo2s)
                    {
                        // don't give up yet, there might be an uncle
                        if(blockInfo2.Uncles.Length > 0)
                        {
                            // fetch all uncles in a single RPC batch request
                            var uncleBatch = blockInfo2.Uncles.Select((x, index) => new DaemonCmd(EC.GetUncleByBlockNumberAndIndex,
                                    new[] { blockInfo2.Height.Value.ToStringHexWithPrefix(), index.ToStringHexWithPrefix() }))
                                .ToArray();

                            logger.Info(() => $"[{LogCategory}] Fetching {blockInfo2.Uncles.Length} uncles for block {blockInfo2.Height}");

                            var uncleResponses = await daemon.ExecuteBatchAnyAsync(logger, uncleBatch);

                            logger.Info(() => $"[{LogCategory}] Fetched {uncleResponses.Count(x => x.Error == null && x.Response != null)} uncles for block {blockInfo2.Height}");

                            var uncle = uncleResponses.Where(x => x.Error == null && x.Response != null)
                                .Select(x => x.Response.ToObject<DaemonResponses.Block>())
                                .FirstOrDefault(x => string.Equals(x.Miner, poolConfig.Address, StringComparison.OrdinalIgnoreCase));

                            if(uncle != null)
                            {
                                // mature?
                                if(latestBlockHeight - uncle.Height.Value >= EthereumConstants.MinConfimations)
                                {
                                    block.Status = BlockStatus.Confirmed;
                                    block.ConfirmationProgress = 1;
                                    block.Reward = GetUncleReward(chainType, uncle.Height.Value, blockInfo2.Height.Value);
                                    block.BlockHeight = uncle.Height.Value;
                                    block.Type = EthereumConstants.BlockTypeUncle;

                                    logger.Info(() => $"[{LogCategory}] Unlocked uncle for block {blockInfo2.Height.Value} at height {uncle.Height.Value} worth {FormatAmount(block.Reward)}");

                                    messageBus.NotifyBlockUnlocked(poolConfig.Id, block, coin);
                                }

                                else
                                    logger.Info(() => $"[{LogCategory}] Got immature matching uncle for block {blockInfo2.Height.Value}. Will try again.");

                                break;
                            }
                        }
                    }

                    if(block.Status == BlockStatus.Pending && block.ConfirmationProgress > 0.75)
                    {
                        // we've lost this one
                        block.Status = BlockStatus.Orphaned;
                        block.Reward = 0;

                        messageBus.NotifyBlockUnlocked(poolConfig.Id, block, coin);
                    }
                }
            }

            return result.ToArray();
        }

        public Task CalculateBlockEffortAsync(Block block, double accumulatedBlockShareDiff)
        {
            block.Effort = accumulatedBlockShareDiff / block.NetworkDifficulty;

            return Task.FromResult(true);
        }

        public override async Task<decimal> UpdateBlockRewardBalancesAsync(IDbConnection con, IDbTransaction tx, Block block, PoolConfig pool)
        {
            var blockRewardRemaining = await base.UpdateBlockRewardBalancesAsync(con, tx, block, pool);

            // Deduct static reserve for tx fees
            blockRewardRemaining -= EthereumConstants.StaticTransactionFeeReserve;

            return blockRewardRemaining;
        }

        public async Task PayoutAsync(Balance[] balances)
        {
            // ensure we have peers
            var infoResponse = await daemon.ExecuteCmdSingleAsync<string>(logger, EC.GetPeerCount);

            if(networkType == EthereumNetworkType.Main &&
                (infoResponse.Error != null || string.IsNullOrEmpty(infoResponse.Response) ||
                    infoResponse.Response.IntegralFromHex<int>() < EthereumConstants.MinPayoutPeerCount))
            {
                logger.Warn(() => $"[{LogCategory}] Payout aborted. Not enough peers (4 required)");
                return;
            }

            var txHashes = new List<string>();

            foreach(var balance in balances)
            {
                try
                {
                    var txHash = await PayoutAsync(balance);
                    txHashes.Add(txHash);
                }

                catch(Exception ex)
                {
                    logger.Error(ex);

                    NotifyPayoutFailure(poolConfig.Id, new[] { balance }, ex.Message, null);
                }
            }

            if(txHashes.Any())
                NotifyPayoutSuccess(poolConfig.Id, balances, txHashes.ToArray(), null);
        }

        #endregion // IPayoutHandler

        private async Task<DaemonResponses.Block[]> FetchBlocks(Dictionary<long, DaemonResponses.Block> blockCache, params long[] blockHeights)
        {
            var cacheMisses = blockHeights.Where(x => !blockCache.ContainsKey(x)).ToArray();

            if(cacheMisses.Any())
            {
                var blockBatch = cacheMisses.Select(height => new DaemonCmd(EC.GetBlockByNumber,
                    new[]
                    {
                        (object) height.ToStringHexWithPrefix(),
                        true
                    })).ToArray();

                var tmp = await daemon.ExecuteBatchAnyAsync(logger, blockBatch);

                var transformed = tmp
                    .Where(x => x.Error == null && x.Response != null)
                    .Select(x => x.Response?.ToObject<DaemonResponses.Block>())
                    .Where(x => x != null)
                    .ToArray();

                foreach(var block in transformed)
                    blockCache[(long) block.Height.Value] = block;
            }

            return blockHeights.Select(x => blockCache[x]).ToArray();
        }

        internal static decimal GetBaseBlockReward(ParityChainType chainType, ulong height)
        {
            switch(chainType)
            {
                case ParityChainType.Mainnet:
                    if(height >= EthereumConstants.ByzantiumHardForkHeight)
                        return EthereumConstants.ByzantiumBlockReward;
                        
                    if(height >= EthereumConstants.ConstantinopleHardForkHeight)
                        return EthereumConstants.ConstantinopleBlockReward;

                    return EthereumConstants.HomesteadBlockReward;

                case ParityChainType.Classic:
                    {
                        var era = Math.Floor(((double) height + 1) / EthereumClassicConstants.BlockPerEra);
                        return (decimal) Math.Pow((double) EthereumClassicConstants.BasePercent, era) * EthereumClassicConstants.BaseRewardInitial;
                    }

                case ParityChainType.Expanse:
                    return EthereumConstants.ExpanseBlockReward;

                case ParityChainType.Ellaism:
                    return EthereumConstants.EllaismBlockReward;

                case ParityChainType.Ropsten:
                    return EthereumConstants.ByzantiumBlockReward;

                case ParityChainType.CallistoTestnet:
                case ParityChainType.Callisto:
                    return CallistoConstants.BaseRewardInitial * (1.0m - CallistoConstants.TreasuryPercent);

                case ParityChainType.Joys:
                    return EthereumConstants.JoysBlockReward;

                default:
                    throw new Exception("Unable to determine block reward: Unsupported chain type");
            }
        }

        private async Task<decimal> GetTxRewardAsync(DaemonResponses.Block blockInfo)
        {
            // fetch all tx receipts in a single RPC batch request
            var batch = blockInfo.Transactions.Select(tx => new DaemonCmd(EC.GetTxReceipt, new[] { tx.Hash }))
                .ToArray();

            var results = await daemon.ExecuteBatchAnyAsync(logger, batch);

            if(results.Any(x => x.Error != null))
                throw new Exception($"Error fetching tx receipts: {string.Join(", ", results.Where(x => x.Error != null).Select(y => y.Error.Message))}");

            // create lookup table
            var gasUsed = results.Select(x => x.Response.ToObject<TransactionReceipt>())
                .ToDictionary(x => x.TransactionHash, x => x.GasUsed);

            // accumulate
            var result = blockInfo.Transactions.Sum(x => (ulong) gasUsed[x.Hash] * ((decimal) x.GasPrice / EthereumConstants.Wei));

            return result;
        }

        internal static decimal GetUncleReward(ParityChainType chainType, ulong uheight, ulong height)
        {
            var reward = GetBaseBlockReward(chainType, height);

            switch(chainType)
            {
                case ParityChainType.Classic:
                    reward *= EthereumClassicConstants.UnclePercent;
                    break;

                default:
                    // https://ethereum.stackexchange.com/a/27195/18000
                    reward *= uheight + 8 - height;
                    reward /= 8m;
                    break;
            }

            return reward;
        }

        private async Task DetectChainAsync()
        {
            var commands = new[]
            {
                new DaemonCmd(EC.GetNetVersion),
                new DaemonCmd(EC.ParityChain),
            };

            var results = await daemon.ExecuteBatchAnyAsync(logger, commands);

            if(results.Any(x => x.Error != null))
            {
                if(results[1].Error != null)
                    isParity = false;

                var errors = results.Take(1).Where(x => x.Error != null)
                    .ToArray();

                if(errors.Any())
                    throw new Exception($"Chain detection failed: {string.Join(", ", errors.Select(y => y.Error.Message))}");
            }

            // convert network
            var netVersion = results[0].Response.ToObject<string>();
            var parityChain = isParity ?
                results[1].Response.ToObject<string>() :
                (extraPoolConfig?.ChainTypeOverride ?? "Mainnet");

            EthereumUtils.DetectNetworkAndChain(netVersion, parityChain, out networkType, out chainType);
        }

        private async Task<string> PayoutAsync(Balance balance)
        {
            // send transaction
            logger.Info(() => $"[{LogCategory}] Sending {FormatAmount(balance.Amount)} to {balance.Address}");

            var amount = (BigInteger)Math.Floor(balance.Amount * EthereumConstants.Wei);

            var request = new SendTransactionRequest
            {
                From = poolConfig.Address,
                To = balance.Address,
                Value = writeHex(amount),
            };

            var response = await daemon.ExecuteCmdSingleAsync<string>(logger, EC.SendTx, new[] { request });

            if(response.Error != null)
                throw new Exception($"{EC.SendTx} returned error: {response.Error.Message} code {response.Error.Code}");

            if(string.IsNullOrEmpty(response.Response) || EthereumConstants.ZeroHashPattern.IsMatch(response.Response))
                throw new Exception($"{EC.SendTx} did not return a valid transaction hash");

            var txHash = response.Response;
            logger.Info(() => $"[{LogCategory}] Payout transaction id: {txHash}");

            // update db
            await PersistPaymentsAsync(new[] { balance }, txHash);

            // done
            return txHash;
        }

        private static string writeHex(BigInteger value)
        {
            return (value.ToString("x").TrimStart('0'));
        }
    }
}
