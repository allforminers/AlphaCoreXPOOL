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
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using Alphaxcore.Blockchain.Ethereum.Configuration;
using Alphaxcore.Configuration;
using Alphaxcore.Extensions;
using Alphaxcore.JsonRpc;
using Alphaxcore.Messaging;
using Alphaxcore.Mining;
using Alphaxcore.Notifications.Messages;
using Alphaxcore.Persistence;
using Alphaxcore.Persistence.Repositories;
using Alphaxcore.Stratum;
using Alphaxcore.Time;
using Alphaxcore.Util;
using Newtonsoft.Json;

namespace Alphaxcore.Blockchain.Ethereum
{
    [CoinFamily(CoinFamily.Ethereum)]
    public class EthereumPool : PoolBase
    {
        public EthereumPool(IComponentContext ctx,
            JsonSerializerSettings serializerSettings,
            IConnectionFactory cf,
            IStatsRepository statsRepo,
            IMapper mapper,
            IMasterClock clock,
            IMessageBus messageBus) :
            base(ctx, serializerSettings, cf, statsRepo, mapper, clock, messageBus)
        {
        }

        private object currentJobParams;
        private EthereumJobManager manager;

        private async Task OnSubscribeAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            if(request.Id == null)
                throw new StratumException(StratumError.Other, "missing request id");

            var requestParams = request.ParamsAs<string[]>();

            if(requestParams == null || requestParams.Length < 2 || requestParams.Any(string.IsNullOrEmpty))
                throw new StratumException(StratumError.MinusOne, "invalid request");

            manager.PrepareWorker(client);

            var data = new object[]
                {
                    new object[]
                    {
                        EthereumStratumMethods.MiningNotify,
                        client.ConnectionId,
                        EthereumConstants.EthereumStratumVersion
                    },
                    context.ExtraNonce1
                }
                .ToArray();

            await client.RespondAsync(data, request.Id);

            // setup worker context
            context.IsSubscribed = true;
            context.UserAgent = requestParams[0].Trim();
            context.IsNiceHashClient = true;
        }

        #region EthereumStratum/1.0.0
        private async Task OnAuthorizeAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            if(request.Id == null)
                throw new StratumException(StratumError.MinusOne, "missing request id");

            var requestParams = request.ParamsAs<string[]>();
            var workerValue = requestParams?.Length > 0 ? requestParams[0] : "0";
            var password = requestParams?.Length > 1 ? requestParams[1] : null;
            var passParts = password?.Split(PasswordControlVarsSeparator);

            // extract worker/miner
            var workerParts = workerValue?.Split('.');
            var minerName = workerParts?.Length > 0 ? workerParts[0].Trim() : null;
            var workerName = workerParts?.Length > 1 ? workerParts[1].Trim() : "0";

            // assumes that workerName is an address
            context.IsAuthorized = !string.IsNullOrEmpty(minerName) && manager.ValidateAddress(minerName);
            context.Miner = minerName;
            context.Worker = workerName;
            context.IsNiceHashClient = true;

            // respond
            await client.RespondAsync(context.IsAuthorized, request.Id);

            // extract control vars from password
            var staticDiff = GetStaticDiffFromPassparts(passParts);
            if(staticDiff.HasValue &&
                (context.VarDiff != null && staticDiff.Value >= context.VarDiff.Config.MinDiff ||
                    context.VarDiff == null && staticDiff.Value > context.Difficulty))
            {
                context.VarDiff = null; // disable vardiff
                context.SetDifficulty(staticDiff.Value);

                logger.Info(() => $"[{client.ConnectionId}] Setting static difficulty of {staticDiff.Value}");
            }

            await EnsureInitialWorkSent(client);

            // log association
            logger.Info(() => $"[{client.ConnectionId}] Authorized worker {workerValue}");
        }

        private async Task EnsureInitialWorkSent(StratumClient client)
        {
            var context = client.ContextAs<EthereumWorkerContext>();
            var sendInitialWork = false;

            lock(context)
            {
                if(context.IsSubscribed && context.IsAuthorized && !context.IsInitialWorkSent && context.IsNiceHashClient)
                {
                    context.IsInitialWorkSent = true;
                    sendInitialWork = true;
                }
            }

            if(sendInitialWork)
            {
                // send intial update
                await client.NotifyAsync(EthereumStratumMethods.SetDifficulty, new object[] { context.Difficulty });
                await client.NotifyAsync(EthereumStratumMethods.MiningNotify, currentJobParams);
            }
        }
        #endregion

        #region Stratum-Proxy
        private async Task OnSubmitLoginAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            if(request.Id == null)
                throw new StratumException(StratumError.MinusOne, "missing request id");

            context.IsSubscribed = true;

            var requestParams = request.ParamsAs<string[]>();
            // setup worker context
            var workerValue = requestParams?.Length > 0 ? requestParams[0] : "0";
            var password = requestParams?.Length > 1 ? requestParams[1] : null;
            var passParts = password?.Split(PasswordControlVarsSeparator);

            // extract worker/miner
            var workerParts = workerValue?.Split('.');
            var minerName = workerParts?.Length > 0 ? workerParts[0].Trim() : null;
            var workerName = workerParts?.Length > 1 ? workerParts[1].Trim() : "0";

            // assumes that workerName is an address
            context.IsAuthorized = !string.IsNullOrEmpty(minerName) && manager.ValidateAddress(minerName);
            context.Miner = minerName.ToLower();
            context.Worker = workerName;
            context.IsNiceHashClient = false;

            // respond
            await client.RespondAsync(context.IsAuthorized, request.Id);

            // extract control vars from password
            var staticDiff = GetStaticDiffFromPassparts(passParts);
            if(staticDiff.HasValue &&
                (context.VarDiff != null && staticDiff.Value >= context.VarDiff.Config.MinDiff ||
                    context.VarDiff == null && staticDiff.Value > context.Difficulty))
            {
                context.VarDiff = null; // disable vardiff
                context.SetDifficulty(staticDiff.Value);

                logger.Info(() => $"[{client.ConnectionId}] Setting static difficulty of {staticDiff.Value}");
            }

            await EnsureInitialWorkSent(client);

            // log association
            logger.Info(() => $"[{client.ConnectionId}] Authorized Stratum-Proxy Worker {workerValue}");
        }

        private async Task OnGetWorkAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            if(request.Id == null)
                throw new StratumException(StratumError.Other, "missing request id");

            object[] newJobParams = (object[]) currentJobParams;
            var header = newJobParams[2];
            var seed = newJobParams[1];
            var target = EthereumUtils.GetTargetHex(new BigInteger(context.Difficulty * EthereumConstants.StratumDiffFactor));

            await client.RespondAsync(new object[] { header, seed, target }, request.Id);
            context.IsInitialWorkSent = true;

            await EnsureInitialWorkSent(client);
        }

        private async Task OnSubmitHashrateAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            if(request.Id == null)
                throw new StratumException(StratumError.Other, "missing request id");

            // Dummy command, just predend like you did something with it and send true to keep the miner happy
            await client.RespondAsync(true, request.Id);

        }

        private async Task OnSubmitAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
        {
            var request = tsRequest.Value;
            var context = client.ContextAs<EthereumWorkerContext>();

            try
            {
                if(request.Id == null)
                    throw new StratumException(StratumError.MinusOne, "missing request id");

                // check age of submission (aged submissions are usually caused by high server load)
                var requestAge = clock.Now - tsRequest.Timestamp.UtcDateTime;

                if(requestAge > maxShareAge)
                {
                    logger.Warn(() => $"[{client.ConnectionId}] Dropping stale share submission request (server overloaded?)");
                    return;
                }

                // validate worker
                if(!context.IsAuthorized)
                    throw new StratumException(StratumError.UnauthorizedWorker, "unauthorized worker");
                else if(!context.IsSubscribed)
                    throw new StratumException(StratumError.NotSubscribed, "not subscribed");

                // check request
                var submitRequest = request.ParamsAs<string[]>();

                if(submitRequest.Length != 3 ||
                    submitRequest.Any(string.IsNullOrEmpty))
                    throw new StratumException(StratumError.MinusOne, "malformed PoW result");

                // recognize activity
                context.LastActivity = clock.Now;

                var poolEndpoint = poolConfig.Ports[client.PoolEndpoint.Port];

                var share = await manager.SubmitShareAsync(client, submitRequest, ct);

                await client.RespondAsync(true, request.Id);

                // publish
                messageBus.SendMessage(new ClientShare(client, share));

                // telemetry
                PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, true);

                logger.Info(() => $"[{client.ConnectionId}] Share accepted: D={Math.Round(share.Difficulty / EthereumConstants.Pow2x32, 3)}");
                await EnsureInitialWorkSent(client);

                // update pool stats
                if(share.IsBlockCandidate)
                    poolStats.LastPoolBlockTime = clock.Now;

                // update client stats
                context.Stats.ValidShares++;
                await UpdateVarDiffAsync(client);
            }

            catch(StratumException ex)
            {
                // telemetry
                PublishTelemetry(TelemetryCategory.Share, clock.Now - tsRequest.Timestamp.UtcDateTime, false);

                // update client stats
                context.Stats.InvalidShares++;
                logger.Info(() => $"[{client.ConnectionId}] Share rejected: {ex.Message}");

                // banning
                ConsiderBan(client, context, poolConfig.Banning);

                throw;
            }
        }
        #endregion

        protected virtual Task OnNewJobAsync(object jobParams)
        {
            currentJobParams = jobParams;

            logger.Info(() => $"Broadcasting job");

            var tasks = ForEachClient(async client =>
            {
                if(!client.IsAlive)
                    return;

                var context = client.ContextAs<EthereumWorkerContext>();

                if(context.IsSubscribed && context.IsAuthorized && context.IsInitialWorkSent && context.IsNiceHashClient)
                {
                    // check alive
                    var lastActivityAgo = clock.Now - context.LastActivity;

                    if(poolConfig.ClientConnectionTimeout > 0 &&
                        lastActivityAgo.TotalSeconds > poolConfig.ClientConnectionTimeout)
                    {
                        logger.Info(() => $"[{client.ConnectionId}] Booting zombie-worker (idle-timeout exceeded)");
                        DisconnectClient(client);
                        return;
                    }

                    // varDiff: if the client has a pending difficulty change, apply it now
                    if(context.ApplyPendingDifficulty())
                        await client.NotifyAsync(EthereumStratumMethods.SetDifficulty, new object[] { context.Difficulty });

                    // send job
                    if(context.IsNiceHashClient)
                        await client.NotifyAsync(EthereumStratumMethods.MiningNotify, currentJobParams);
                }
            });

            return Task.WhenAll(tasks);
        }

        #region Overrides

        protected override async Task SetupJobManager(CancellationToken ct)
        {
            manager = ctx.Resolve<EthereumJobManager>();
            manager.Configure(poolConfig, clusterConfig);

            await manager.StartAsync(ct);

            if(poolConfig.EnableInternalStratum == true)
            {
                disposables.Add(manager.Jobs
                    .Select(job => Observable.FromAsync(async () =>
                    {
                        try
                        {
                            await OnNewJobAsync(job);
                        }

                        catch(Exception ex)
                        {
                            logger.Debug(() => $"{nameof(OnNewJobAsync)}: {ex.Message}");
                        }
                    }))
                    .Concat()
                    .Subscribe(_ => { }, ex =>
                    {
                        logger.Debug(ex, nameof(OnNewJobAsync));
                    }));

                // we need work before opening the gates
                await manager.Jobs.Take(1).ToTask(ct);
            }

            else
            {
                // keep updating NetworkStats
                disposables.Add(manager.Jobs.Subscribe());
            }
        }

        protected override async Task InitStatsAsync()
        {
            await base.InitStatsAsync();

            blockchainStats = manager.BlockchainStats;
        }

        protected override WorkerContextBase CreateClientContext()
        {
            return new EthereumWorkerContext();
        }

        protected override async Task OnRequestAsync(StratumClient client,
            Timestamped<JsonRpcRequest> tsRequest, CancellationToken ct)
        {
            var request = tsRequest.Value;

            try
            {
                switch(request.Method)
                {
                    #region EthereumStratum/1.0.0
                    case EthereumStratumMethods.Subscribe:
                        await OnSubscribeAsync(client, tsRequest);
                        break;

                    case EthereumStratumMethods.Authorize:
                        await OnAuthorizeAsync(client, tsRequest);
                        break;

                    case EthereumStratumMethods.SubmitShare:
                        await OnSubmitAsync(client, tsRequest, ct);
                        break;

                    case EthereumStratumMethods.ExtraNonceSubscribe:
                        await client.RespondErrorAsync(StratumError.Other, "not supported", request.Id, false);
                        break;
                    #endregion

                    #region Stratum-Proxy
                    case EthereumStratumMethods.SubmitLogin:
                        await OnSubmitLoginAsync(client, tsRequest);
                        break;

                    case EthereumStratumMethods.GetWork:
                        await OnGetWorkAsync(client, tsRequest);
                        break;

                    case EthereumStratumMethods.SubmitHasrate:
                        await OnSubmitHashrateAsync(client, tsRequest);
                        break;

                    case EthereumStratumMethods.SubmitWork:
                        await OnSubmitAsync(client, tsRequest, ct);
                        break;
                    #endregion  

                    default:
                        logger.Debug(() => $"[{client.ConnectionId}] Unsupported RPC request: {JsonConvert.SerializeObject(request, serializerSettings)}");

                        await client.RespondErrorAsync(StratumError.Other, $"Unsupported request {request.Method}", request.Id);
                        break;
                }
            }

            catch(StratumException ex)
            {
                await client.RespondErrorAsync(ex.Code, ex.Message, request.Id, false);
            }
        }

        public override double HashrateFromShares(double shares, double interval)
        {
            var result = shares / interval;
            return result;
        }

        protected override async Task OnVarDiffUpdateAsync(StratumClient client, double newDiff)
        {
            await base.OnVarDiffUpdateAsync(client, newDiff);

            // apply immediately and notify client
            var context = client.ContextAs<EthereumWorkerContext>();

            if(context.HasPendingDifficulty)
            {
                context.ApplyPendingDifficulty();

                // send job
                await client.NotifyAsync(EthereumStratumMethods.SetDifficulty, new object[] { context.Difficulty });

                if(context.IsNiceHashClient)
                    await client.NotifyAsync(EthereumStratumMethods.MiningNotify, currentJobParams);
            }
        }

        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {
            base.Configure(poolConfig, clusterConfig);

            // validate mandatory extra config
            var extraConfig = poolConfig.PaymentProcessing?.Extra?.SafeExtensionDataAs<EthereumPoolPaymentProcessingConfigExtra>();
            if(clusterConfig.PaymentProcessing?.Enabled == true && extraConfig?.CoinbasePassword == null)
                logger.ThrowLogPoolStartupException("\"paymentProcessing.coinbasePassword\" pool-configuration property missing or empty (required for unlocking wallet during payment processing)");
        }

        #endregion // Overrides
    }
}
