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
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using NBitcoin;
using Contract = Alphaxcore.Contracts.Contract;

namespace Alphaxcore.Banning
{
    public class IntegratedBanManager : IBanManager
    {
        private static readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(10)
        });

        #region Implementation of IBanManager

        public bool IsBanned(IPAddress address)
        {
            var result = cache.Get(address.ToString());
            return result != null;
        }

        public void Ban(IPAddress address, TimeSpan duration)
        {
            Contract.RequiresNonNull(address, nameof(address));
            Contract.Requires<ArgumentException>(duration.TotalMilliseconds > 0, $"{nameof(duration)} must not be empty");

            // don't ban 127.0.0.1
            if(address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback))
                return;

            cache.Set(address.ToString(), string.Empty, duration);
        }

        #endregion
    }
}
