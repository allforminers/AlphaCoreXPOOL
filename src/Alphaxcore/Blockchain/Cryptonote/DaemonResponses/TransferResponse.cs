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

namespace Alphaxcore.Blockchain.Cryptonote.DaemonResponses
{
    public class TransferResponse
    {
        /// <summary>
        /// Integer value of the fee charged for the txn (piconeros)
        /// </summary>
        public ulong Fee { get; set; }

        /// <summary>
        /// String for the transaction key if get_tx_key is true, otherwise, blank string.
        /// </summary>
        [JsonProperty("tx_key")]
        public string TxKey { get; set; }

        /// <summary>
        /// Publically searchable transaction hash
        /// </summary>
        [JsonProperty("tx_hash")]
        public string TxHash { get; set; }

        /// <summary>
        /// Raw transaction represented as hex string, if get_tx_hex is true.
        /// </summary>
        [JsonProperty("tx_blob")]
        public string TxBlob { get; set; }

        /// <summary>
        /// (Optional) If true, the newly created transaction will not be relayed to the monero network
        /// </summary>
        [JsonProperty("do_not_relay")]
        public string DoNotRelay { get; set; }

        public string Status { get; set; }
    }
}
