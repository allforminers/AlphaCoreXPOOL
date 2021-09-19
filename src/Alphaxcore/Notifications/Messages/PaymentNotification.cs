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
    public class PaymentNotification
    {
        public PaymentNotification(string poolId, string error, decimal amount, string symbol, int recpientsCount, string[] txIds, string[] txExplorerLinks, decimal? txFee)
        {
            PoolId = poolId;
            Error = error;
            Amount = amount;
            RecpientsCount = recpientsCount;
            TxIds = txIds;
            TxFee = txFee;
            Symbol = symbol;
            TxExplorerLinks = txExplorerLinks;
        }

        public PaymentNotification(string poolId, string error, decimal amount, string symbol) : this(poolId, error, amount, symbol, 0, null, null, null)
        {
        }

        public PaymentNotification()
        {
        }

        public string PoolId { get; set; }
        public decimal? TxFee { get; set; }
        public string[] TxIds { get; set; }
        public string[] TxExplorerLinks { get; set; }
        public string Symbol { get; set; }
        public int RecpientsCount { get; set; }
        public decimal Amount { get; set; }
        public string Error { get; set; }
    }
}
