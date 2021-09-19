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
using System.Runtime.CompilerServices;
using System.Text;
using NLog;

namespace Alphaxcore.Extensions
{
    public static class LoggingExtensions
    {
        public static void LogInvoke(this ILogger logger, object[] args = null, [CallerMemberName] string caller = null)
        {
            if(args == null)
                logger.Debug(() => $"{caller}()");
            else
                logger.Debug(() => $"{caller}({string.Join(", ", args.Select(x => x?.ToString()))})");
        }

        public static void LogInvoke(this ILogger logger, string logCat, object[] args = null, [CallerMemberName] string caller = null)
        {
            if(args == null)
                logger.Debug(() => $"[{logCat}] {caller}()");
            else
                logger.Debug(() => $"[{logCat}] {caller}({string.Join(", ", args.Select(x => x?.ToString()))})");
        }
    }
}
