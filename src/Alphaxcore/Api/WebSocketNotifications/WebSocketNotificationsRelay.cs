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
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Alphaxcore.Api.WebSocketNotifications;
using Alphaxcore.Configuration;
using Alphaxcore.Extensions;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebSocketManager;
using WebSocketManager.Common;

namespace Alphaxcore.Api
{
    public class WebSocketNotificationsRelay : WebSocketHandler
    {
        public WebSocketNotificationsRelay(WebSocketConnectionManager webSocketConnectionManager, IComponentContext ctx) :
            base(webSocketConnectionManager, new StringMethodInvocationStrategy())
        {
            messageBus = ctx.Resolve<IMessageBus>();
            clusterConfig = ctx.Resolve<ClusterConfig>();
            pools = clusterConfig.Pools.ToDictionary(x => x.Id, x => x);

            serializer = new JsonSerializer
            {
                ContractResolver = ctx.Resolve<JsonSerializerSettings>().ContractResolver
            };

            Relay<BlockFoundNotification>(WsNotificationType.BlockFound);
            Relay<BlockUnlockedNotification>(WsNotificationType.BlockUnlocked);
            Relay<BlockConfirmationProgressNotification>(WsNotificationType.BlockUnlockProgress);
            Relay<NewChainHeightNotification>(WsNotificationType.NewChainHeight);
            Relay<PaymentNotification>(WsNotificationType.Payment);
            Relay<HashrateNotification>(WsNotificationType.HashrateUpdated);
        }

        private IMessageBus messageBus;
        private readonly ClusterConfig clusterConfig;
        private readonly Dictionary<string, PoolConfig> pools;
        private JsonSerializer serializer;
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public override async Task OnConnected(WebSocket socket)
        {
            WebSocketConnectionManager.AddSocket(socket);

            var greeting = ToJson(WsNotificationType.Greeting, new { Message = "Connected to Alphaxcore notification relay" });
            await socket.SendAsync(greeting, CancellationToken.None);
        }

        private void Relay<T>(WsNotificationType type)
        {
            messageBus.Listen<T>()
                .Select(x => Observable.FromAsync(() => BroadcastNotification(type, x)))
                .Concat()
                .Subscribe();
        }

        private async Task BroadcastNotification<T>(WsNotificationType type, T notification)
        {
            try
            {
                var json = ToJson(type, notification);

                var msg = new Message
                {
                    MessageType = MessageType.TextRaw,
                    Data = json
                };

                await SendMessageToAllAsync(msg);
            }

            catch(Exception ex)
            {
                logger.Error(ex);
            }
        }

        private string ToJson<T>(WsNotificationType type, T msg)
        {
            var result = JObject.FromObject(msg, serializer);
            result["type"] = type.ToString().ToLower();

            return result.ToString(Formatting.None);
        }
    }
}
