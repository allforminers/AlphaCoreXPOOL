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
using Alphaxcore.Extensions;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Alphaxcore.Blockchain.Equihash
{
    public class EquihashBlockHeader : IBitcoinSerializable
    {
        public EquihashBlockHeader(string hex)
            : this(Encoders.Hex.DecodeData(hex))
        {
        }

        public EquihashBlockHeader(byte[] bytes)
        {
            this.ReadWrite(new BitcoinStream(bytes));
        }

        public EquihashBlockHeader()
        {
            SetNull();
        }

        private uint256 hashMerkleRoot;
        private uint256 hashPrevBlock;
        private byte[] hashReserved = new byte[32];
        private uint nBits;
        private string nNonce;
        private uint nTime;
        private int nVersion;
        private string nSolution;

        // header
        private const int CURRENT_VERSION = 4;

        public uint256 HashPrevBlock
        {
            get => hashPrevBlock;
            set => hashPrevBlock = value;
        }

        public Target Bits
        {
            get => nBits;
            set => nBits = value;
        }

        public int Version
        {
            get => nVersion;
            set => nVersion = value;
        }

        public string Nonce
        {
            get => nNonce;
            set => nNonce = value;
        }
        
        public string SolutionIn
        {
            get => nSolution;
            set => nSolution = value;
        }

        public uint256 HashMerkleRoot
        {
            get => hashMerkleRoot;
            set => hashMerkleRoot = value;
        }

        public byte[] HashReserved
        {
            get => hashReserved;
            set => hashReserved = value;
        }

        public bool IsNull => nBits == 0;

        public uint NTime
        {
            get => nTime;
            set => nTime = value;
        }

        public DateTimeOffset BlockTime
        {
            get => Utils.UnixTimeToDateTime(nTime);
            set => nTime = Utils.DateTimeToUnixTime(value);
        }

        #region IBitcoinSerializable Members

        public void ReadWrite(BitcoinStream stream)
        {
            var nonceBytes = nNonce.HexToByteArray();
            var solutionBytes = nSolution.HexToByteArray();

            stream.ReadWrite(ref nVersion);
            stream.ReadWrite(ref hashPrevBlock);
            stream.ReadWrite(ref hashMerkleRoot);
            stream.ReadWrite(ref hashReserved);
            stream.ReadWrite(ref nTime);
            stream.ReadWrite(ref nBits);
            stream.ReadWrite(ref nonceBytes);
            stream.ReadWrite(ref solutionBytes);
        }

        #endregion

        public static EquihashBlockHeader Parse(string hex)
        {
            return new EquihashBlockHeader(Encoders.Hex.DecodeData(hex));
        }

        internal void SetNull()
        {
            nVersion = CURRENT_VERSION;
            hashPrevBlock = 0;
            hashMerkleRoot = 0;
            hashReserved = new byte[32];
            nTime = 0;
            nBits = 0;
            nNonce = string.Empty;
            nSolution = string.Empty;
        }
    }
}
