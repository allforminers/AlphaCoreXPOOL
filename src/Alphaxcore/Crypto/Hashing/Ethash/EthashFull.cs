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
using System.Threading;
using System.Threading.Tasks;
using Alphaxcore.Blockchain.Ethereum;
using Alphaxcore.Contracts;
using NLog;

namespace Alphaxcore.Crypto.Hashing.Ethash
{
    public class EthashFull : IDisposable
    {
        public EthashFull(int numCaches, string dagDir)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(dagDir), $"{nameof(dagDir)} must not be empty");

            this.numCaches = numCaches;
            this.dagDir = dagDir;
        }

        private int numCaches; // Maximum number of caches to keep before eviction (only init, don't modify)
        private readonly object cacheLock = new object();
        private readonly Dictionary<ulong, Dag> caches = new Dictionary<ulong, Dag>();
        private Dag future;
        private readonly string dagDir;

        public void Dispose()
        {
            foreach(var value in caches.Values)
                value.Dispose();
        }

        public async Task<Dag> GetDagAsync(ulong block, ILogger logger, CancellationToken ct)
        {
            var epoch = block / EthereumConstants.EpochLength;
            Dag result;

            lock(cacheLock)
            {
                if(numCaches == 0)
                    numCaches = 3;

                if(!caches.TryGetValue(epoch, out result))
                {
                    // No cached DAG, evict the oldest if the cache limit was reached
                    while(caches.Count >= numCaches)
                    {
                        var toEvict = caches.Values.OrderBy(x => x.LastUsed).First();
                        var key = caches.First(pair => pair.Value == toEvict).Key;
                        var epochToEvict = toEvict.Epoch;

                        logger.Info(() => $"Evicting DAG for epoch {epochToEvict} in favour of epoch {epoch}");
                        toEvict.Dispose();
                        caches.Remove(key);
                    }

                    // If we have the new DAG pre-generated, use that, otherwise create a new one
                    if(future != null && future.Epoch == epoch)
                    {
                        logger.Debug(() => $"Using pre-generated DAG for epoch {epoch}");

                        result = future;
                        future = null;
                    }

                    else
                    {
                        logger.Info(() => $"No pre-generated DAG available, creating new for epoch {epoch}");
                        result = new Dag(epoch);
                    }

                    caches[epoch] = result;
                }

                // If we used up the future cache, or need a refresh, regenerate
                else if(future == null || future.Epoch <= epoch)
                {
                    logger.Info(() => $"Pre-generating DAG for epoch {epoch + 1}");
                    future = new Dag(epoch + 1);

#pragma warning disable 4014
                    future.GenerateAsync(dagDir, logger, ct);
#pragma warning restore 4014
                }

                result.LastUsed = DateTime.Now;
            }

            await result.GenerateAsync(dagDir, logger, ct);
            return result;
        }
    }
}
