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

namespace Alphaxcore.Blockchain.Cryptonote.DaemonRequests
{
    public class TransferDestination
    {
        public string Address { get; set; }
        public ulong Amount { get; set; }
    }

    public class TransferRequest
    {
        public TransferDestination[] Destinations { get; set; }

        /// <summary>
        /// Number of outpouts from the blockchain to mix with (0 means no mixing)
        /// </summary>
        public uint Mixin { get; set; }

        /// <summary>
        /// Set a priority for the transaction. Accepted Values are: 0-3 for: default, unimportant, normal, elevated, priority
        /// </summary>
        public uint Priority { get; set; }

        /// <summary>
        /// Number of outputs to mix in the transaction (this output + N decoys from the blockchain)
        /// </summary>
        [JsonProperty("ring_size")]
        public uint RingSize { get; set; } = 7;

        /// <summary>
        /// (Optional) Random 32-byte/64-character hex string to identify a transaction
        /// </summary>
        [JsonProperty("payment_id")]
        public string PaymentId { get; set; }

        /// <summary>
        /// (Optional) Return the transaction key after sending
        /// </summary>
        [JsonProperty("get_tx_key")]
        public bool GetTxKey { get; set; }

        /// <summary>
        /// (Optional) Return the transaction key after sending
        /// </summary>
        [JsonProperty("get_tx_hex")]
        public bool GetTxHex { get; set; }

        /// <summary>
        /// Number of blocks before the monero can be spent (0 to not add a lock)
        /// </summary>
        [JsonProperty("unlock_time")]
        public uint UnlockTime { get; set; }
    }
}
