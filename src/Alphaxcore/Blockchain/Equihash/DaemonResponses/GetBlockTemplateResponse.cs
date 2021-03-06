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

using Newtonsoft.Json;

namespace Alphaxcore.Blockchain.Equihash.DaemonResponses
{
    public class EquihashCoinbaseTransaction
    {
        public string Data { get; set; }
        public string Hash { get; set; }
        public decimal Fee { get; set; }
        public int SigOps { get; set; }
        public ulong FoundersReward { get; set; }
        public bool Required { get; set; }

        // "depends":[ ],
    }

    public class EquihashBlockTemplate : Bitcoin.DaemonResponses.BlockTemplate
    {
        public string[] Capabilities { get; set; }

        [JsonProperty("coinbasetxn")]
        public EquihashCoinbaseTransaction CoinbaseTx { get; set; }

        public string LongPollId { get; set; }
        public ulong MinTime { get; set; }
        public ulong SigOpLimit { get; set; }
        public ulong SizeLimit { get; set; }
        public string[] Mutable { get; set; }

        public ZCashBlockSubsidy Subsidy { get; set; }

        [JsonProperty("finalsaplingroothash")]
        public string FinalSaplingRootHash { get; set; }
        
        [JsonProperty("solution")]
        public string Solution { get; set; }
    }
}
