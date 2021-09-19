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
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using Alphaxcore.Blockchain.Bitcoin;
using Alphaxcore.Blockchain.Bitcoin.DaemonResponses;
using Alphaxcore.Blockchain.Equihash.Configuration;
using Alphaxcore.Blockchain.Equihash.DaemonRequests;
using Alphaxcore.Blockchain.Equihash.DaemonResponses;
using Alphaxcore.Configuration;
using Alphaxcore.Extensions;
using Alphaxcore.Messaging;
using Alphaxcore.Persistence;
using Alphaxcore.Persistence.Model;
using Alphaxcore.Persistence.Repositories;
using Alphaxcore.Time;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Contract = Alphaxcore.Contracts.Contract;
using IBlockRepository = Alphaxcore.Persistence.Repositories.IBlockRepository;

namespace Alphaxcore.Blockchain.Equihash
{
    [CoinFamily(CoinFamily.Equihash)]
    public class EquihashPayoutHandler : BitcoinPayoutHandler
    {
        public EquihashPayoutHandler(
            IComponentContext ctx,
            IConnectionFactory cf,
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            IMasterClock clock,
            IMessageBus messageBus) :
            base(ctx, cf, mapper, shareRepo, blockRepo, balanceRepo, paymentRepo, clock, messageBus)
        {
        }

        protected EquihashPoolConfigExtra poolExtraConfig;
        protected bool supportsNativeShielding;
        protected Network network;
        protected EquihashCoinTemplate.EquihashNetworkParams chainConfig;
        protected override string LogCategory => "Equihash Payout Handler";
        protected const decimal TransferFee = 0.0001m;
        protected const int ZMinConfirmations = 8;

        #region IPayoutHandler

        public override async Task ConfigureAsync(ClusterConfig clusterConfig, PoolConfig poolConfig)
        {
            await base.ConfigureAsync(clusterConfig, poolConfig);

            poolExtraConfig = poolConfig.Extra.SafeExtensionDataAs<EquihashPoolConfigExtra>();

            // detect network
            var blockchainInfoResponse = await daemon.ExecuteCmdSingleAsync<BlockchainInfo>(logger, BitcoinCommands.GetBlockchainInfo);

            network = Network.GetNetwork(blockchainInfoResponse.Response.Chain.ToLower());

            chainConfig = poolConfig.Template.As<EquihashCoinTemplate>().GetNetwork(network.NetworkType);

            // detect z_shieldcoinbase support
            var response = await daemon.ExecuteCmdSingleAsync<JObject>(logger, EquihashCommands.ZShieldCoinbase);
            supportsNativeShielding = response.Error.Code != (int) BitcoinRPCErrorCode.RPC_METHOD_NOT_FOUND;
        }

        public override async Task PayoutAsync(Balance[] balances)
        {
            Contract.RequiresNonNull(balances, nameof(balances));

            // Shield first
            if(supportsNativeShielding)
                await ShieldCoinbaseAsync();
            else
                await ShieldCoinbaseEmulatedAsync();

            var didUnlockWallet = false;

            // send in batches with no more than 50 recipients to avoid running into tx size limits
            var pageSize = 50;
            var pageCount = (int) Math.Ceiling(balances.Length / (double) pageSize);

            for(var i = 0; i < pageCount; i++)
            {
                didUnlockWallet = false;

                // get a page full of balances
                var page = balances
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToArray();

                // build args
                var amounts = page
                    .Where(x => x.Amount > 0)
                    .Select(x => new ZSendManyRecipient { Address = x.Address, Amount = Math.Round(x.Amount, 8) })
                    .ToList();

                if(amounts.Count == 0)
                    return;

                var pageAmount = amounts.Sum(x => x.Amount);

                // check shielded balance
                var balanceResult = await daemon.ExecuteCmdSingleAsync<object>(logger, EquihashCommands.ZGetBalance, new object[]
                {
                    poolExtraConfig.ZAddress, // default account
                    ZMinConfirmations, // only spend funds covered by this many confirmations
                });

                if(balanceResult.Error != null || (decimal) (double) balanceResult.Response - TransferFee < pageAmount)
                {
                    logger.Info(() => $"[{LogCategory}] Insufficient shielded balance for payment of {FormatAmount(pageAmount)}");
                    return;
                }

                logger.Info(() => $"[{LogCategory}] Paying out {FormatAmount(pageAmount)} to {page.Length} addresses");

                var args = new object[]
                {
                    poolExtraConfig.ZAddress, // default account
                    amounts, // addresses and associated amounts
                    ZMinConfirmations, // only spend funds covered by this many confirmations
                    TransferFee
                };

            // send command
            tryTransfer:
                var result = await daemon.ExecuteCmdSingleAsync<string>(logger, EquihashCommands.ZSendMany, args);

                if(result.Error == null)
                {
                    var operationId = result.Response;

                    // check result
                    if(string.IsNullOrEmpty(operationId))
                        logger.Error(() => $"[{LogCategory}] {EquihashCommands.ZSendMany} did not return a operation id!");
                    else
                    {
                        logger.Info(() => $"[{LogCategory}] Tracking payout operation id: {operationId}");

                        var continueWaiting = true;

                        while(continueWaiting)
                        {
                            var operationResultResponse = await daemon.ExecuteCmdSingleAsync<ZCashAsyncOperationStatus[]>(logger,
                                EquihashCommands.ZGetOperationResult, new object[] { new object[] { operationId } });

                            if(operationResultResponse.Error == null &&
                                operationResultResponse.Response?.Any(x => x.OperationId == operationId) == true)
                            {
                                var operationResult = operationResultResponse.Response.First(x => x.OperationId == operationId);

                                if(!Enum.TryParse(operationResult.Status, true, out ZOperationStatus status))
                                {
                                    logger.Error(() => $"Unrecognized operation status: {operationResult.Status}");
                                    break;
                                }

                                switch(status)
                                {
                                    case ZOperationStatus.Success:
                                        var txId = operationResult.Result?.Value<string>("txid") ?? string.Empty;
                                        logger.Info(() => $"[{LogCategory}] {EquihashCommands.ZSendMany} completed with transaction id: {txId}");

                                        await PersistPaymentsAsync(page, txId);
                                        NotifyPayoutSuccess(poolConfig.Id, page, new[] { txId }, null);

                                        continueWaiting = false;
                                        continue;

                                    case ZOperationStatus.Cancelled:
                                    case ZOperationStatus.Failed:
                                        logger.Error(() => $"{EquihashCommands.ZSendMany} failed: {operationResult.Error.Message} code {operationResult.Error.Code}");
                                        NotifyPayoutFailure(poolConfig.Id, page, $"{EquihashCommands.ZSendMany} failed: {operationResult.Error.Message} code {operationResult.Error.Code}", null);

                                        continueWaiting = false;
                                        continue;
                                }
                            }

                            logger.Info(() => $"[{LogCategory}] Waiting for completion: {operationId}");
                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                }

                else
                {
                    if(result.Error.Code == (int) BitcoinRPCErrorCode.RPC_WALLET_UNLOCK_NEEDED && !didUnlockWallet)
                    {
                        if(!string.IsNullOrEmpty(extraPoolPaymentProcessingConfig?.WalletPassword))
                        {
                            logger.Info(() => $"[{LogCategory}] Unlocking wallet");

                            var unlockResult = await daemon.ExecuteCmdSingleAsync<JToken>(logger, BitcoinCommands.WalletPassphrase, new[]
                            {
                                (object) extraPoolPaymentProcessingConfig.WalletPassword,
                                (object) 5 // unlock for N seconds
                            });

                            if(unlockResult.Error == null)
                            {
                                didUnlockWallet = true;
                                goto tryTransfer;
                            }

                            else
                            {
                                logger.Error(() => $"[{LogCategory}] {BitcoinCommands.WalletPassphrase} returned error: {result.Error.Message} code {result.Error.Code}");
                                NotifyPayoutFailure(poolConfig.Id, page, $"{BitcoinCommands.WalletPassphrase} returned error: {result.Error.Message} code {result.Error.Code}", null);
                                break;
                            }
                        }

                        else
                        {
                            logger.Error(() => $"[{LogCategory}] Wallet is locked but walletPassword was not configured. Unable to send funds.");
                            NotifyPayoutFailure(poolConfig.Id, page, $"Wallet is locked but walletPassword was not configured. Unable to send funds.", null);
                            break;
                        }
                    }

                    else
                    {
                        logger.Error(() => $"[{LogCategory}] {EquihashCommands.ZSendMany} returned error: {result.Error.Message} code {result.Error.Code}");

                        NotifyPayoutFailure(poolConfig.Id, page, $"{EquihashCommands.ZSendMany} returned error: {result.Error.Message} code {result.Error.Code}", null);
                    }
                }
            }

            // lock wallet
            logger.Info(() => $"[{LogCategory}] Locking wallet");
            await daemon.ExecuteCmdSingleAsync<JToken>(logger, BitcoinCommands.WalletLock);
        }

        #endregion // IPayoutHandler

        /// <summary>
        /// ZCash coins are mined into a t-addr (transparent address), but can only be
        /// spent to a z-addr (shielded address), and must be swept out of the t-addr
        /// in one transaction with no change.
        /// </summary>
        private async Task ShieldCoinbaseAsync()
        {
            logger.Info(() => $"[{LogCategory}] Shielding ZCash Coinbase funds");

            var args = new object[]
            {
                poolConfig.Address, // source: pool's t-addr receiving coinbase rewards
                poolExtraConfig.ZAddress, // dest:   pool's z-addr
            };

            var result = await daemon.ExecuteCmdSingleAsync<ZCashShieldingResponse>(logger, EquihashCommands.ZShieldCoinbase, args);

            if(result.Error != null)
            {
                if(result.Error.Code == -6)
                    logger.Info(() => $"[{LogCategory}] No funds to shield");
                else
                    logger.Error(() => $"[{LogCategory}] {EquihashCommands.ZShieldCoinbase} returned error: {result.Error.Message} code {result.Error.Code}");

                return;
            }

            var operationId = result.Response.OperationId;

            logger.Info(() => $"[{LogCategory}] {EquihashCommands.ZShieldCoinbase} operation id: {operationId}");

            var continueWaiting = true;

            while(continueWaiting)
            {
                var operationResultResponse = await daemon.ExecuteCmdSingleAsync<ZCashAsyncOperationStatus[]>(logger,
                    EquihashCommands.ZGetOperationResult, new object[] { new object[] { operationId } });

                if(operationResultResponse.Error == null &&
                    operationResultResponse.Response?.Any(x => x.OperationId == operationId) == true)
                {
                    var operationResult = operationResultResponse.Response.First(x => x.OperationId == operationId);

                    if(!Enum.TryParse(operationResult.Status, true, out ZOperationStatus status))
                    {
                        logger.Error(() => $"Unrecognized operation status: {operationResult.Status}");
                        break;
                    }

                    switch(status)
                    {
                        case ZOperationStatus.Success:
                            logger.Info(() => $"[{LogCategory}] {EquihashCommands.ZShieldCoinbase} successful");

                            continueWaiting = false;
                            continue;

                        case ZOperationStatus.Cancelled:
                        case ZOperationStatus.Failed:
                            logger.Error(() => $"{EquihashCommands.ZShieldCoinbase} failed: {operationResult.Error.Message} code {operationResult.Error.Code}");

                            continueWaiting = false;
                            continue;
                    }
                }

                logger.Info(() => $"[{LogCategory}] Waiting for shielding operation completion: {operationId}");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private async Task ShieldCoinbaseEmulatedAsync()
        {
            logger.Info(() => $"[{LogCategory}] Shielding ZCash Coinbase funds (emulated)");

            // get t-addr unspent balance for just the coinbase address (pool wallet)
            var unspentResult = await daemon.ExecuteCmdSingleAsync<Utxo[]>(logger, BitcoinCommands.ListUnspent);

            if(unspentResult.Error != null)
            {
                logger.Error(() => $"[{LogCategory}] {BitcoinCommands.ListUnspent} returned error: {unspentResult.Error.Message} code {unspentResult.Error.Code}");
                return;
            }

            var balance = unspentResult.Response
                .Where(x => x.Spendable && x.Address == poolConfig.Address)
                .Sum(x => x.Amount);

            // make sure there's enough balance to shield after reserves
            if(balance - TransferFee <= TransferFee)
            {
                logger.Info(() => $"[{LogCategory}] Balance {FormatAmount(balance)} too small for emulated shielding");
                return;
            }

            logger.Info(() => $"[{LogCategory}] Transferring {FormatAmount(balance - TransferFee)} to pool's z-addr");

            // transfer to z-addr
            var recipient = new ZSendManyRecipient
            {
                Address = poolExtraConfig.ZAddress,
                Amount = balance - TransferFee
            };

            var args = new object[]
            {
                poolConfig.Address, // default account
                new object[] // addresses and associated amounts
                {
                    recipient
                },
                1,
                TransferFee
            };

            // send command
            var sendResult = await daemon.ExecuteCmdSingleAsync<string>(logger, EquihashCommands.ZSendMany, args);

            if(sendResult.Error != null)
            {
                logger.Error(() => $"[{LogCategory}] {EquihashCommands.ZSendMany} returned error: {unspentResult.Error.Message} code {unspentResult.Error.Code}");
                return;
            }

            var operationId = sendResult.Response;

            logger.Info(() => $"[{LogCategory}] {EquihashCommands.ZSendMany} operation id: {operationId}");

            var continueWaiting = true;

            while(continueWaiting)
            {
                var operationResultResponse = await daemon.ExecuteCmdSingleAsync<ZCashAsyncOperationStatus[]>(logger,
                    EquihashCommands.ZGetOperationResult, new object[] { new object[] { operationId } });

                if(operationResultResponse.Error == null &&
                    operationResultResponse.Response?.Any(x => x.OperationId == operationId) == true)
                {
                    var operationResult = operationResultResponse.Response.First(x => x.OperationId == operationId);

                    if(!Enum.TryParse(operationResult.Status, true, out ZOperationStatus status))
                    {
                        logger.Error(() => $"Unrecognized operation status: {operationResult.Status}");
                        break;
                    }

                    switch(status)
                    {
                        case ZOperationStatus.Success:
                            var txId = operationResult.Result?.Value<string>("txid") ?? string.Empty;
                            logger.Info(() => $"[{LogCategory}] Transfer completed with transaction id: {txId}");

                            continueWaiting = false;
                            continue;

                        case ZOperationStatus.Cancelled:
                        case ZOperationStatus.Failed:
                            logger.Error(() => $"{EquihashCommands.ZSendMany} failed: {operationResult.Error.Message} code {operationResult.Error.Code}");

                            continueWaiting = false;
                            continue;
                    }
                }

                logger.Info(() => $"[{LogCategory}] Waiting for shielding transfer completion: {operationId}");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
