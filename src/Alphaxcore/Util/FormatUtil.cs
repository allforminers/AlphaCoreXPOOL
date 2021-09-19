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

namespace Alphaxcore.Util
{
    public static class FormatUtil
    {
        public static readonly string[] HashrateUnits = { " KH/s", " MH/s", " GH/s", " TH/s", " PH/s" };
        public static readonly string[] DifficultyUnits = { " K", " M", " G", " T", " P" };
        public static readonly string[] CapacityUnits = { " KB", " MB", " GB", " TB", " PB" };
        public static readonly string[] QuantityUnits = { "K", "M", "B", "T", "Q" };

        public static string FormatHashrate(double hashrate)
        {
            var i = -1;

            do
            {
                hashrate = hashrate / 1024;
                i++;
            } while(hashrate > 1024 && i < HashrateUnits.Length - 1);

            return (int) Math.Abs(hashrate) + HashrateUnits[i];
        }

        public static string FormatDifficulty(double difficulty)
        {
            var i = -1;

            do
            {
                difficulty = difficulty / 1024;
                i++;
            } while(difficulty > 1024);

            return (int) Math.Abs(difficulty) + DifficultyUnits[i];
        }

        public static string FormatCapacity(double hashrate)
        {
            var i = -1;

            do
            {
                hashrate = hashrate / 1024;
                i++;
            } while(hashrate > 1024 && i < CapacityUnits.Length - 1);

            return (int) Math.Abs(hashrate) + CapacityUnits[i];
        }

        public static string FormatQuantity(double value)
        {
            var i = -1;

            do
            {
                value = value / 1000;
                i++;
            } while(value > 1000);

            return Math.Round(value, 2) + DifficultyUnits[i];
        }
    }
}
