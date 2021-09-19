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
using System.Linq;
using Autofac;
using Autofac.Core.Registration;
using Newtonsoft.Json.Linq;

namespace Alphaxcore.Crypto.Hashing.Equihash
{
    public static class EquihashSolverFactory
    {
        private const string HashName = "equihash";
        private static readonly ConcurrentDictionary<string, EquihashSolver> cache = new ConcurrentDictionary<string, EquihashSolver>();

        public static EquihashSolver GetSolver(IComponentContext ctx, JObject definition)
        {
            var hash = definition["hash"]?.Value<string>().ToLower();

            if(string.IsNullOrEmpty(hash) || hash != HashName)
                throw new NotSupportedException($"Invalid hash value '{hash}'. Expected '{HashName}'");

            var args = definition["args"]?
                .Select(token => token.Value<object>())
                .ToArray();

            if(args?.Length != 3)
                throw new NotSupportedException($"Invalid hash arguments '{string.Join(", ", args)}'");

            return InstantiateSolver(ctx, args);
        }

        private static EquihashSolver InstantiateSolver(IComponentContext ctx, object[] args)
        {
            var key = string.Join("-", args);
            if(cache.TryGetValue(key, out var result))
                return result;

            var n = (int) Convert.ChangeType(args[0], typeof(int));
            var k = (int) Convert.ChangeType(args[1], typeof(int));
            var personalization = args[2].ToString();

            // Lookup type
            var hashClass = (typeof(EquihashSolver).Namespace + $".EquihashSolver_{n}_{k}");
            var hashType = typeof(EquihashSolver).Assembly.GetType(hashClass, true);

            try
            {
                // create it (we'll let Autofac do the heavy lifting)
                result = (EquihashSolver) ctx.Resolve(hashType, new PositionalParameter(0, personalization));
            }

            catch(ComponentNotRegisteredException)
            {
                throw new NotSupportedException($"Equihash variant {n}_{k} is currently not implemented");
            }

            cache.TryAdd(key, result);
            return result;
        }
    }
}
