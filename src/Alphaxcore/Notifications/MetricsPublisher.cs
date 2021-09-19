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
using Alphaxcore.Configuration;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications.Messages;
using Prometheus;

namespace Alphaxcore.Notifications
{
    public class MetricsPublisher
    {
        public MetricsPublisher(IMessageBus messageBus)
        {
            CreateMetrics();

            messageBus.Listen<TelemetryEvent>().Subscribe(OnTelemetryEvent);
        }

        private Summary btStreamLatencySummary;
        private Counter shareCounter;
        private Summary rpcRequestDurationSummary;

        private void CreateMetrics()
        {
            btStreamLatencySummary = Metrics.CreateSummary("miningcore_btstream_latency", "Latency of streaming block-templates in ms", new SummaryConfiguration
            {
                LabelNames = new[] { "pool" }
            });

            shareCounter = Metrics.CreateCounter("miningcore_valid_shares_total", "Valid received shares per pool", new CounterConfiguration
            {
                LabelNames = new[] { "pool" }
            });

            rpcRequestDurationSummary = Metrics.CreateSummary("miningcore_rpcrequest_execution_time", "Duration of RPC requests ms", new SummaryConfiguration
            {
                LabelNames = new[] { "pool", "method" }
            });
        }

        private void OnTelemetryEvent(TelemetryEvent msg)
        {
            switch(msg.Category)
            {
                case TelemetryCategory.Share:
                    shareCounter.WithLabels(msg.PoolId).Inc();
                    break;

                case TelemetryCategory.BtStream:
                    btStreamLatencySummary.WithLabels(msg.PoolId).Observe(msg.Elapsed.TotalMilliseconds);
                    break;

                case TelemetryCategory.RpcRequest:
                    rpcRequestDurationSummary.WithLabels(msg.PoolId, msg.Info).Observe(msg.Elapsed.TotalMilliseconds);
                    break;
            }
        }
    }
}
