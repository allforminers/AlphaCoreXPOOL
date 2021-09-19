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
using System.Text;

namespace Alphaxcore.Notifications.Messages
{
    public enum TelemetryCategory
    {
        Share = 1, // Share processed
        BtStream, // Blocktemplate over BTStream
        RpcRequest // JsonRPC Request to Daemon
    }

    public class TelemetryEvent
    {
        public TelemetryEvent(string server, string poolId, TelemetryCategory category, TimeSpan elapsed, bool? success = null, string error = null)
        {
            Server = server;
            PoolId = poolId;
            Category = category;
            Elapsed = elapsed;
            Success = success;
            Error = error;
        }

        public TelemetryEvent(string server, string poolId, TelemetryCategory category, string info, TimeSpan elapsed, bool? success = null, string error = null) :
            this(server, poolId, category, elapsed, success, error)
        {
            Info = info;
        }

        public string Server { get; set; }
        public string PoolId { get; set; }
        public TelemetryCategory Category { get; set; }
        public string Info { get; }
        public TimeSpan Elapsed { get; set; }
        public bool? Success { get; set; }
        public string Error { get; }
    }
}
