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
using System.Globalization;
using System.Numerics;
using Autofac;
using Alphaxcore.Blockchain.Bitcoin;
using Alphaxcore.Crypto;
using Alphaxcore.Crypto.Hashing.Algorithms;
using NBitcoin;
using Newtonsoft.Json;

namespace Alphaxcore.Configuration
{
    public abstract partial class CoinTemplate
    {
        public T As<T>() where T : CoinTemplate
        {
            return (T) this;
        }

        public abstract string GetAlgorithmName();

        /// <summary>
        /// json source file where this template originated from
        /// </summary>
        [JsonIgnore]
        public string Source { get; set; }
    }

    public partial class BitcoinTemplate
    {
        public BitcoinTemplate()
        {
            coinbaseHasherValue = new Lazy<IHashAlgorithm>(() =>
            {
                if(CoinbaseHasher == null)
                    return null;

                return HashAlgorithmFactory.GetHash(ComponentContext, CoinbaseHasher);
            });

            headerHasherValue = new Lazy<IHashAlgorithm>(() =>
            {
                if(HeaderHasher == null)
                    return null;

                return HashAlgorithmFactory.GetHash(ComponentContext, HeaderHasher);
            });

            blockHasherValue = new Lazy<IHashAlgorithm>(() =>
            {
                if(BlockHasher == null)
                    return null;

                return HashAlgorithmFactory.GetHash(ComponentContext, BlockHasher);
            });

            posBlockHasherValue = new Lazy<IHashAlgorithm>(() =>
            {
                if(PoSBlockHasher == null)
                    return null;

                return HashAlgorithmFactory.GetHash(ComponentContext, PoSBlockHasher);
            });
        }

        private readonly Lazy<IHashAlgorithm> coinbaseHasherValue;
        private readonly Lazy<IHashAlgorithm> headerHasherValue;
        private readonly Lazy<IHashAlgorithm> blockHasherValue;
        private readonly Lazy<IHashAlgorithm> posBlockHasherValue;

        public IComponentContext ComponentContext { get; set; }

        public IHashAlgorithm CoinbaseHasherValue => coinbaseHasherValue.Value;
        public IHashAlgorithm HeaderHasherValue => headerHasherValue.Value;
        public IHashAlgorithm BlockHasherValue => blockHasherValue.Value;
        public IHashAlgorithm PoSBlockHasherValue => posBlockHasherValue.Value;

        public BitcoinNetworkParams GetNetwork(NetworkType networkType)
        {
            if(Networks == null || Networks.Count == 0)
                return null;

            switch(networkType)
            {
                case NetworkType.Mainnet:
                    return Networks["main"];
                case NetworkType.Testnet:
                    return Networks["test"];
                case NetworkType.Regtest:
                    return Networks["regtest"];
            }

            throw new NotSupportedException("unsupported network type");
        }

        #region Overrides of CoinTemplate

        public override string GetAlgorithmName()
        {
            var hash = HeaderHasherValue;

            if(hash.GetType() == typeof(DigestReverser))
                return ((DigestReverser) hash).Upstream.GetType().Name;

            return hash.GetType().Name;
        }

        #endregion
    }

    public partial class EquihashCoinTemplate
    {
        public partial class EquihashNetworkParams
        {
            public EquihashNetworkParams()
            {
                diff1Value = new Lazy<NBitcoin.BouncyCastle.Math.BigInteger>(() =>
                {
                    if(string.IsNullOrEmpty(Diff1))
                        throw new InvalidOperationException("Diff1 has not yet been initialized");

                    return new NBitcoin.BouncyCastle.Math.BigInteger(Diff1, 16);
                });

                diff1BValue = new Lazy<BigInteger>(() =>
                {
                    if(string.IsNullOrEmpty(Diff1))
                        throw new InvalidOperationException("Diff1 has not yet been initialized");

                    return BigInteger.Parse(Diff1, NumberStyles.HexNumber);
                });
            }

            private readonly Lazy<NBitcoin.BouncyCastle.Math.BigInteger> diff1Value;
            private readonly Lazy<BigInteger> diff1BValue;

            [JsonIgnore]
            public NBitcoin.BouncyCastle.Math.BigInteger Diff1Value => diff1Value.Value;

            [JsonIgnore]
            public BigInteger Diff1BValue => diff1BValue.Value;

            [JsonIgnore]
            public ulong FoundersRewardSubsidySlowStartShift => FoundersRewardSubsidySlowStartInterval / 2;

            [JsonIgnore]
            public ulong LastFoundersRewardBlockHeight => FoundersRewardSubsidyHalvingInterval + FoundersRewardSubsidySlowStartShift - 1;
        }

        public EquihashNetworkParams GetNetwork(NetworkType networkType)
        {
            switch(networkType)
            {
                case NetworkType.Mainnet:
                    return Networks["main"];
                case NetworkType.Testnet:
                    return Networks["test"];
                case NetworkType.Regtest:
                    return Networks["regtest"];
            }

            throw new NotSupportedException("unsupported network type");
        }

        #region Overrides of CoinTemplate

        public override string GetAlgorithmName()
        {
            // TODO: return variant
            return "Equihash";
        }

        #endregion
    }

    public partial class CryptonoteCoinTemplate
    {
        #region Overrides of CoinTemplate

        public override string GetAlgorithmName()
        {
            switch(Hash)
            {
                case CryptonightHashType.Normal:
                    return "Cryptonight";
                case CryptonightHashType.Lite:
                    return "Cryptonight-Lite";
                case CryptonightHashType.Heavy:
                    return "Cryptonight-Heavy";
            }

            throw new NotSupportedException("Invalid hash type");
        }

        #endregion
    }

    public partial class EthereumCoinTemplate
    {
        #region Overrides of CoinTemplate

        public override string GetAlgorithmName()
        {
            return "Ethash";
        }

        #endregion
    }

    public partial class PoolConfig
    {
        /// <summary>
        /// Back-reference to coin template for this pool
        /// </summary>
        [JsonIgnore]
        public CoinTemplate Template { get; set; }
    }
}
