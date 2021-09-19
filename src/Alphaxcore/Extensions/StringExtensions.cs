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
using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Alphaxcore.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a hex string to byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexToByteArray(this string str)
        {
            if(str.StartsWith("0x"))
                str = str.Substring(2);

            var arr = new byte[str.Length >> 1];
            var count = str.Length >> 1;

            for(var i = 0; i < count; ++i)
                arr[i] = (byte) ((GetHexVal(str[i << 1]) << 4) + GetHexVal(str[(i << 1) + 1]));

            return arr;
        }

        /// <summary>
        /// Converts a hex string to byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexToReverseByteArray(this string str)
        {
            if(str.StartsWith("0x"))
                str = str.Substring(2);

            var arr = new byte[str.Length >> 1];
            var count = str.Length >> 1;

            for(var i = 0; i < count; ++i)
                arr[count - 1 - i] = (byte) ((GetHexVal(str[i << 1]) << 4) + GetHexVal(str[(i << 1) + 1]));

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            var val = (int) hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static string ToStringHex8(this uint value)
        {
            return value.ToString("x8", CultureInfo.InvariantCulture);
        }

        public static string ToStringHex8(this int value)
        {
            return value.ToString("x8", CultureInfo.InvariantCulture);
        }

        public static string ToStringHexWithPrefix(this ulong value)
        {
            if(value == 0)
                return "0x0";

            return "0x" + value.ToString("x", CultureInfo.InvariantCulture);
        }

        public static string ToStringHexWithPrefix(this long value)
        {
            if(value == 0)
                return "0x0";

            return "0x" + value.ToString("x", CultureInfo.InvariantCulture);
        }

        public static string ToStringHexWithPrefix(this uint value)
        {
            if(value == 0)
                return "0x0";

            return "0x" + value.ToString("x", CultureInfo.InvariantCulture);
        }

        public static string ToStringHexWithPrefix(this int value)
        {
            if(value == 0)
                return "0x0";

            return "0x" + value.ToString("x", CultureInfo.InvariantCulture);
        }

        public static string StripHexPrefix(this string value)
        {
            if(value?.ToLower().StartsWith("0x") == true)
                return value.Substring(2);

            return value;
        }

        public static T IntegralFromHex<T>(this string value)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));

            if(value.StartsWith("0x"))
                value = value.Substring(2);

            if(!ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var val))
                throw new FormatException();

            return (T) Convert.ChangeType(val, underlyingType ?? typeof(T));
        }

        public static string ToLowerCamelCase(this string str)
        {
            if(string.IsNullOrEmpty(str))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string AsString(this ReadOnlySequence<byte> line, Encoding encoding)
        {
            return encoding.GetString(line.ToSpan());
        }

        public static string Capitalize(this string str)
        {
            if(string.IsNullOrEmpty(str))
                return str;

            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }
    }
}
