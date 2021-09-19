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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Alphaxcore.Blockchain;
using Alphaxcore.Configuration;
using Alphaxcore.Extensions;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications.Messages;
using Alphaxcore.Persistence;
using Alphaxcore.Persistence.Model;
using Alphaxcore.Persistence.Repositories;
using Alphaxcore.Time;
using Newtonsoft.Json;
using NLog;
using Polly;
using Contract = Alphaxcore.Contracts.Contract;

namespace Alphaxcore.Payments
{
    public abstract class PayoutHandlerBase
    {
        protected PayoutHandlerBase(
            IConnectionFactory cf,
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            IMasterClock clock,
            IMessageBus messageBus)
        {
            Contract.RequiresNonNull(cf, nameof(cf));
            Contract.RequiresNonNull(mapper, nameof(mapper));
            Contract.RequiresNonNull(shareRepo, nameof(shareRepo));
            Contract.RequiresNonNull(blockRepo, nameof(blockRepo));
            Contract.RequiresNonNull(balanceRepo, nameof(balanceRepo));
            Contract.RequiresNonNull(paymentRepo, nameof(paymentRepo));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.cf = cf;
            this.mapper = mapper;
            this.clock = clock;
            this.shareRepo = shareRepo;
            this.blockRepo = blockRepo;
            this.balanceRepo = balanceRepo;
            this.paymentRepo = paymentRepo;
            this.messageBus = messageBus;

            BuildFaultHandlingPolicy();
        }

        protected readonly IBalanceRepository balanceRepo;
        protected readonly IBlockRepository blockRepo;
        protected readonly IConnectionFactory cf;
        protected readonly IMapper mapper;
        protected readonly IPaymentRepository paymentRepo;
        protected readonly IShareRepository shareRepo;
        protected readonly IMasterClock clock;
        protected readonly IMessageBus messageBus;
        protected ClusterConfig clusterConfig;
        private IAsyncPolicy faultPolicy;

        protected ILogger logger;
        protected PoolConfig poolConfig;
        private const int RetryCount = 8;

        protected abstract string LogCategory { get; }

        protected void BuildFaultHandlingPolicy()
        {
            var retry = Policy
                .Handle<DbException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), OnRetry);

            faultPolicy = retry;
        }

        protected virtual void OnRetry(Exception ex, TimeSpan timeSpan, int retry, object context)
        {
            logger.Warn(() => $"[{LogCategory}] Retry {1} in {timeSpan} due to: {ex}");
        }

        public virtual async Task<decimal> UpdateBlockRewardBalancesAsync(IDbConnection con, IDbTransaction tx, Block block, PoolConfig pool)
        {
            var blockRewardRemaining = block.Reward;

            // Distribute funds to configured reward recipients
            foreach(var recipient in poolConfig.RewardRecipients.Where(x => x.Percentage > 0))
            {
                var amount = block.Reward * (recipient.Percentage / 100.0m);
                var address = recipient.Address;

                blockRewardRemaining -= amount;

                // skip transfers from pool wallet to pool wallet
                if(address != poolConfig.Address)
                {
                    logger.Info(() => $"Adding {FormatAmount(amount)} to balance of {address}");
                    await balanceRepo.AddAmountAsync(con, tx, poolConfig.Id, address, amount, $"Reward for block {block.BlockHeight}");
                }
            }

            return blockRewardRemaining;
        }

        protected virtual async Task PersistPaymentsAsync(Balance[] balances, string transactionConfirmation)
        {
            var coin = poolConfig.Template.As<CoinTemplate>();

            try
            {
                await faultPolicy.ExecuteAsync(async () =>
                {
                    await cf.RunTx(async (con, tx) =>
                    {
                        foreach(var balance in balances)
                        {
                            if(!string.IsNullOrEmpty(transactionConfirmation) &&
                                !poolConfig.RewardRecipients.Any(x => x.Address == balance.Address))
                            {
                                // record payment
                                var payment = new Payment
                                {
                                    PoolId = poolConfig.Id,
                                    Coin = coin.Symbol,
                                    Address = balance.Address,
                                    Amount = balance.Amount,
                                    Created = clock.Now,
                                    TransactionConfirmationData = transactionConfirmation
                                };

                                await paymentRepo.InsertAsync(con, tx, payment);
                            }

                            // reset balance
                            logger.Debug(() => $"[{LogCategory}] Resetting balance of {balance.Address}");
                            await balanceRepo.AddAmountAsync(con, tx, poolConfig.Id, balance.Address, -balance.Amount, $"Balance reset after payment");
                        }
                    });
                });
            }

            catch(Exception ex)
            {
                logger.Error(ex, () => $"[{LogCategory}] Failed to persist the following payments: " +
                    $"{JsonConvert.SerializeObject(balances.Where(x => x.Amount > 0).ToDictionary(x => x.Address, x => x.Amount))}");
                throw;
            }
        }

        public string FormatAmount(decimal amount)
        {
            var coin = poolConfig.Template.As<CoinTemplate>();
            return $"{amount:0.#####} {coin.Symbol}";
        }

        protected virtual void NotifyPayoutSuccess(string poolId, Balance[] balances, string[] txHashes, decimal? txFee)
        {
            var coin = poolConfig.Template.As<CoinTemplate>();

            // admin notifications
            var explorerLinks = !string.IsNullOrEmpty(coin.ExplorerTxLink) ?
                txHashes.Select(x => string.Format(coin.ExplorerTxLink, x)).ToArray() :
                new string[0];

            messageBus.SendMessage(new PaymentNotification(poolId, null, balances.Sum(x => x.Amount), coin.Symbol, balances.Length, txHashes, explorerLinks, txFee));
        }

        protected virtual void NotifyPayoutFailure(string poolId, Balance[] balances, string error, Exception ex)
        {
            var coin = poolConfig.Template.As<CoinTemplate>();

            messageBus.SendMessage(new PaymentNotification(poolId, error ?? ex?.Message, balances.Sum(x => x.Amount), coin.Symbol));
        }
    }
}
