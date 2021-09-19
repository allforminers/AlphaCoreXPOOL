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
using System.Threading.Tasks;
using Alphaxcore.Blockchain.Ethereum;
using Alphaxcore.Contracts;
using Alphaxcore.Extensions;
using Alphaxcore.Native;
using NLog;

namespace Alphaxcore.Crypto.Hashing.Ethash
{
    public class Cache : IDisposable
    {
        public Cache(ulong epoch)
        {
            Epoch = epoch;
            LastUsed = DateTime.Now;
        }

        private IntPtr handle = IntPtr.Zero;
        private bool isGenerated = false;
        private readonly object genLock = new object();

        public ulong Epoch { get; }
        public DateTime LastUsed { get; set; }

        public void Dispose()
        {
            if(handle != IntPtr.Zero)
            {
                LibMultihash.ethash_light_delete(handle);
                handle = IntPtr.Zero;
            }
        }

        public async Task GenerateAsync(ILogger logger)
        {
            await Task.Run(() =>
            {
                lock(genLock)
                {
                    if(!isGenerated)
                    {
                        var started = DateTime.Now;
                        logger.Debug(() => $"Generating cache for epoch {Epoch}");

                        var block = Epoch * EthereumConstants.EpochLength;
                        handle = LibMultihash.ethash_light_new(block);

                        logger.Debug(() => $"Done generating cache for epoch {Epoch} after {DateTime.Now - started}");
                        isGenerated = true;
                    }
                }
            });
        }

        public unsafe bool Compute(ILogger logger, byte[] hash, ulong nonce, out byte[] mixDigest, out byte[] result)
        {
            Contract.RequiresNonNull(hash, nameof(hash));

            logger.LogInvoke();

            mixDigest = null;
            result = null;

            var value = new LibMultihash.ethash_return_value();

            fixed (byte* input = hash)
            {
                LibMultihash.ethash_light_compute(handle, input, nonce, ref value);
            }

            if(value.success)
            {
                mixDigest = value.mix_hash.value;
                result = value.result.value;
            }

            return value.success;
        }
    }
}
