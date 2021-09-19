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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Alphaxcore.Crypto.Hashing.Algorithms;
using Newtonsoft.Json.Linq;

namespace Alphaxcore.Crypto
{
    public static class HashAlgorithmFactory
    {
        private static readonly ConcurrentDictionary<string, IHashAlgorithm> cache = new ConcurrentDictionary<string, IHashAlgorithm>();

        public static IHashAlgorithm GetHash(IComponentContext ctx, JObject definition)
        {
            var hash = definition["hash"]?.Value<string>().ToLower();

            if(string.IsNullOrEmpty(hash))
                throw new NotSupportedException("$Invalid or empty hash value {hash}");

            var args = definition["args"]?
                .Select(token => token.Type == JTokenType.Object ? GetHash(ctx, (JObject) token) : token.Value<object>())
                .ToArray();

            return InstantiateHash(ctx, hash, args);
        }

        private static IHashAlgorithm InstantiateHash(IComponentContext ctx, string name, object[] args)
        {
            // special handling for DigestReverser
            if(name == "reverse")
                name = nameof(DigestReverser);

            // check cache if possible
            var hasArgs = args != null && args.Length > 0;
            if(!hasArgs && cache.TryGetValue(name, out var result))
                return result;

            var hashClass = (typeof(Sha256D).Namespace + "." + name).ToLower();
            var hashType = typeof(Sha256D).Assembly.GetType(hashClass, true, true);

            // create it (we'll let Autofac do the heavy lifting)
            if(hasArgs)
                result = (IHashAlgorithm) ctx.Resolve(hashType, args.Select((x, i) => new PositionalParameter(i, x)));
            else
            {
                result = (IHashAlgorithm) ctx.Resolve(hashType);
                cache.TryAdd(name, result);
            }

            return result;
        }
    }
}
