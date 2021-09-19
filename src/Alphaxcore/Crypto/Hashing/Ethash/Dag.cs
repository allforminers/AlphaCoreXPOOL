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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alphaxcore.Blockchain.Ethereum;
using Alphaxcore.Contracts;
using Alphaxcore.Extensions;
using Alphaxcore.Native;
using NLog;

namespace Alphaxcore.Crypto.Hashing.Ethash
{
    public class Dag : IDisposable
    {
        public Dag(ulong epoch)
        {
            Epoch = epoch;
        }

        public ulong Epoch { get; set; }

        private IntPtr handle = IntPtr.Zero;
        private static readonly Semaphore sem = new Semaphore(1, 1);

        public DateTime LastUsed { get; set; }

        public static unsafe string GetDefaultDagDirectory()
        {
            var chars = new byte[512];

            fixed (byte* data = chars)
            {
                if(LibMultihash.ethash_get_default_dirname(data, chars.Length))
                {
                    int length;
                    for(length = 0; length < chars.Length; length++)
                    {
                        if(data[length] == 0)
                            break;
                    }

                    return Encoding.UTF8.GetString(data, length);
                }
            }

            return null;
        }

        public void Dispose()
        {
            if(handle != IntPtr.Zero)
            {
                LibMultihash.ethash_full_delete(handle);
                handle = IntPtr.Zero;
            }
        }

        public async ValueTask GenerateAsync(string dagDir, ILogger logger, CancellationToken ct)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(dagDir), $"{nameof(dagDir)} must not be empty");

            if(handle == IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        sem.WaitOne();

                        // re-check after obtaining lock
                        if(handle != IntPtr.Zero)
                            return;

                        logger.Info(() => $"Generating DAG for epoch {Epoch}");

                        var started = DateTime.Now;
                        var block = Epoch * EthereumConstants.EpochLength;

                        // Generate a temporary cache
                        var light = LibMultihash.ethash_light_new(block);

                        try
                        {
                            // Generate the actual DAG
                            handle = LibMultihash.ethash_full_new(dagDir, light, progress =>
                            {
                                logger.Info(() => $"Generating DAG for epoch {Epoch}: {progress}%");

                                return !ct.IsCancellationRequested ? 0 : 1;
                            });

                            if(handle == IntPtr.Zero)
                                throw new OutOfMemoryException("ethash_full_new IO or memory error");

                            logger.Info(() => $"Done generating DAG for epoch {Epoch} after {DateTime.Now - started}");
                        }

                        finally
                        {
                            if(light != IntPtr.Zero)
                                LibMultihash.ethash_light_delete(light);
                        }
                    }

                    finally
                    {
                        sem.Release();
                    }
                }, ct);
            }
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
                LibMultihash.ethash_full_compute(handle, input, nonce, ref value);
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
