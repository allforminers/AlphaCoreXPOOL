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
using System.Numerics;
using System.Text.RegularExpressions;

namespace Alphaxcore.Blockchain.Ethereum
{
    public class EthereumConstants
    {
        public const ulong EpochLength = 30000;
        public const ulong CacheSizeForTesting = 1024;
        public const ulong DagSizeForTesting = 1024 * 32;
        public static BigInteger BigMaxValue = BigInteger.Pow(2, 256);
        public static double Pow2x32 = Math.Pow(2, 32);
        public static BigInteger BigPow2x32 = new BigInteger(Pow2x32);
        public const int AddressLength = 20;
        public const decimal Wei = 1000000000000000000;
        public static BigInteger WeiBig = new BigInteger(1000000000000000000);
        public const string EthereumStratumVersion = "EthereumStratum/1.0.0";
        public const decimal StaticTransactionFeeReserve = 0.0025m; // in ETH
        public const string BlockTypeUncle = "uncle";
        public static double StratumDiffFactor = 4294901760.0;

#if !DEBUG
        public const int MinPayoutPeerCount = 1;
#else
        public const int MinPayoutPeerCount = 1;
#endif

        public static readonly Regex ValidAddressPattern = new Regex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled);
        public static readonly Regex ZeroHashPattern = new Regex("^0?x?0+$", RegexOptions.Compiled);
        public static readonly Regex NoncePattern = new Regex("^0x[0-9a-f]{16}$", RegexOptions.Compiled);
        public static readonly Regex HashPattern = new Regex("^0x[0-9a-f]{64}$", RegexOptions.Compiled);
        public static readonly Regex WorkerPattern = new Regex("^[0-9a-zA-Z-_]{1,8}$", RegexOptions.Compiled);

        public const ulong ByzantiumHardForkHeight = 4370000;
        public const ulong  ConstantinopleHardForkHeight = 7280000;
        public const decimal HomesteadBlockReward = 5.0m;
        public const decimal ByzantiumBlockReward = 3.0m;
        public const decimal ConstantinopleBlockReward = 2.0m;
        public const decimal TestnetBlockReward = 2.0m;
        public const decimal ExpanseBlockReward = 2.0m;
        public const decimal EllaismBlockReward = 2.0m;
        public const decimal JoysBlockReward = 2.0m;


        public const int MinConfimations = 16;
    }

    public class EthereumClassicConstants
    {
        public const decimal BaseRewardInitial = 5m;
        public const decimal BasePercent = 0.8m;
        public const int BlockPerEra = 5000000;
        public const decimal UnclePercent = 0.03125m;
    }

    public class CallistoConstants
    {
        public const decimal BaseRewardInitial = 600m;
        public const decimal TreasuryPercent = 0.3m;
    }

    public enum EthereumNetworkType
    {
        Main = 1,
        Morden = 2,
        Ropsten = 3,
        Rinkeby = 4,
        Kovan = 42,
        Galilei = 7919,
        Joys = 35855456,

        Unknown = -1,
    }

    public enum ParityChainType
    {
        Foundation,
        Olympic,
        Frontier,
        Homestead,
        Mainnet,
        Morden,
        Ropsten,
        Classic,
        Expanse,
        Ellaism,
        CallistoTestnet,
        Callisto,
        Joys,

        Unknown = 1,
    }

    public static class EthCommands
    {
        public const string GetWork = "eth_getWork";
        public const string SubmitWork = "eth_submitWork";
        public const string Sign = "eth_sign";
        public const string GetNetVersion = "net_version";
        public const string GetClientVersion = "web3_clientVersion";
        public const string GetCoinbase = "eth_coinbase";
        public const string GetAccounts = "eth_accounts";
        public const string GetPeerCount = "net_peerCount";
        public const string GetSyncState = "eth_syncing";
        public const string GetBlockByNumber = "eth_getBlockByNumber";
        public const string GetBlockByHash = "eth_getBlockByHash";
        public const string GetUncleByBlockNumberAndIndex = "eth_getUncleByBlockNumberAndIndex";
        public const string GetTxReceipt = "eth_getTransactionReceipt";
        public const string SendTx = "eth_sendTransaction";
        public const string UnlockAccount = "personal_unlockAccount";
        public const string Subscribe = "eth_subscribe";
        public const string ParityVersion = "parity_versionInfo";
        public const string ParityChain = "parity_chain";
        public const string ParitySubscribe = "parity_subscribe";
    }
}
