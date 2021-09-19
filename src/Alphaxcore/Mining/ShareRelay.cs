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
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Alphaxcore.Blockchain;
using Alphaxcore.Configuration;
using Alphaxcore.Contracts;
using Alphaxcore.Extensions;
using Alphaxcore.Messaging;
using Alphaxcore.Util;
using Newtonsoft.Json;
using NLog;
using ProtoBuf;
using ZeroMQ;

namespace Alphaxcore.Mining
{
    public class ShareRelay
    {
        public ShareRelay(JsonSerializerSettings serializerSettings, IMessageBus messageBus)
        {
            Contract.RequiresNonNull(serializerSettings, nameof(serializerSettings));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.serializerSettings = serializerSettings;
            this.messageBus = messageBus;
        }

        private readonly IMessageBus messageBus;
        private ClusterConfig clusterConfig;
        private readonly BlockingCollection<Share> queue = new BlockingCollection<Share>();
        private IDisposable queueSub;
        private readonly int QueueSizeWarningThreshold = 1024;
        private bool hasWarnedAboutBacklogSize;
        private ZSocket pubSocket;
        private readonly JsonSerializerSettings serializerSettings;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        [Flags]
        public enum WireFormat
        {
            Json = 1,
            ProtocolBuffers = 2
        }

        public const int WireFormatMask = 0xF;

        #region API-Surface

        public void Start(ClusterConfig clusterConfig)
        {
            this.clusterConfig = clusterConfig;

            messageBus.Listen<ClientShare>().Subscribe(x => queue.Add(x.Share));

            pubSocket = new ZSocket(ZSocketType.PUB);

            if(!clusterConfig.ShareRelay.Connect)
            {
                pubSocket.SetupCurveTlsServer(clusterConfig.ShareRelay.SharedEncryptionKey, logger);

                pubSocket.Bind(clusterConfig.ShareRelay.PublishUrl);

                if(pubSocket.CurveServer)
                    logger.Info(() => $"Bound to {clusterConfig.ShareRelay.PublishUrl} using Curve public-key {pubSocket.CurvePublicKey.ToHexString()}");
                else
                    logger.Info(() => $"Bound to {clusterConfig.ShareRelay.PublishUrl}");
            }

            else
            {
                if(!string.IsNullOrEmpty(clusterConfig.ShareRelay.SharedEncryptionKey?.Trim()))
                    logger.ThrowLogPoolStartupException("ZeroMQ Curve is not supported in ShareRelay Connect-Mode");

                pubSocket.Connect(clusterConfig.ShareRelay.PublishUrl);
                logger.Info(() => $"Connected to {clusterConfig.ShareRelay.PublishUrl}");
            }

            InitializeQueue();

            logger.Info(() => "Online");
        }

        public void Stop()
        {
            logger.Info(() => "Stopping ..");

            pubSocket.Dispose();

            queueSub?.Dispose();
            queueSub = null;

            logger.Info(() => "Stopped");
        }

        #endregion // API-Surface

        private void InitializeQueue()
        {
            queueSub = queue.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .Do(_ => CheckQueueBacklog())
                .Subscribe(share =>
                {
                    share.Source = clusterConfig.ClusterName;
                    share.BlockRewardDouble = (double) share.BlockReward;

                    try
                    {
                        var flags = (int) WireFormat.ProtocolBuffers;

                        using(var msg = new ZMessage())
                        {
                            // Topic frame
                            msg.Add(new ZFrame(share.PoolId));

                            // Frame 2: flags
                            msg.Add(new ZFrame(flags));

                            // Frame 3: payload
                            using(var stream = new MemoryStream())
                            {
                                Serializer.Serialize(stream, share);
                                msg.Add(new ZFrame(stream.ToArray()));
                            }

                            pubSocket.SendMessage(msg);
                        }
                    }

                    catch(Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
        }

        private void CheckQueueBacklog()
        {
            if(queue.Count > QueueSizeWarningThreshold)
            {
                if(!hasWarnedAboutBacklogSize)
                {
                    logger.Warn(() => $"Share relay queue backlog has crossed {QueueSizeWarningThreshold}");
                    hasWarnedAboutBacklogSize = true;
                }
            }

            else if(hasWarnedAboutBacklogSize && queue.Count <= QueueSizeWarningThreshold / 2)
            {
                hasWarnedAboutBacklogSize = false;
            }
        }
    }
}
