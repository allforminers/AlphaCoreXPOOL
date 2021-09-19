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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alphaxcore.Blockchain.Bitcoin.DaemonResponses
{
    public class Masternode
    {
        public string Payee { get; set; }
        public string Script { get; set; }
        public long Amount { get; set; }
    }

    public class SuperBlock
    {
        public string Payee { get; set; }
        public long Amount { get; set; }
    }

    public class MasterNodeBlockTemplateExtra : PayeeBlockTemplateExtra
    {
        public JToken Masternode { get; set; }

        [JsonProperty("indexnode")]
        private JToken Indexnode { set { Masternode = value; } }

        [JsonProperty("fivegnode")]
        private JToken Fivegnode { set { Masternode = value; } }
        
        [JsonProperty("shroudnode")]
        private JToken Shroudnode { set { Masternode = value; } }
        
        [JsonProperty("smartnode")]
        private JToken Smartnode { set { Masternode = value; } }
        
        [JsonProperty("masternode_payments_started")]
        public bool MasternodePaymentsStarted { get; set; }

        [JsonProperty("masternode_payments_enforced")]
        public bool MasternodePaymentsEnforced { get; set; }

        [JsonProperty("superblock")]
        public SuperBlock[] SuperBlocks { get; set; }

        [JsonProperty("superblocks_started")]
        public bool SuperblocksStarted { get; set; }

        [JsonProperty("superblocks_enabled")]
        public bool SuperblocksEnabled { get; set; }

        [JsonProperty("coinbase_payload")]
        public string CoinbasePayload { get; set; }
    }
}
