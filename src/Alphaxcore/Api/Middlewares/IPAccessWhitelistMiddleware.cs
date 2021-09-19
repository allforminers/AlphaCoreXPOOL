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

using Microsoft.AspNetCore.Http;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Alphaxcore.Api.Middlewares
{
    public class IPAccessWhitelistMiddleware
    {
        public IPAccessWhitelistMiddleware(RequestDelegate next, string[] locations, IPAddress[] whitelist)
        {
            this.whitelist = whitelist;
            this.next = next;
            this.locations = locations;
        }

        private readonly RequestDelegate next;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IPAddress[] whitelist;
        private readonly string[] locations;

        public async Task Invoke(HttpContext context)
        {
            if(locations.Any(x => context.Request.Path.Value.StartsWith(x)))
            {
                var remoteAddress = context.Connection.RemoteIpAddress;

                if(!whitelist.Any(x => x.Equals(remoteAddress)))
                {
                    logger.Info(() => $"Unauthorized request attempt to {context.Request.Path.Value} from {remoteAddress}");

                    context.Response.StatusCode = (int) HttpStatusCode.Forbidden;
                    await context.Response.WriteAsync("You are not in my access list. Good Bye.\n");
                    return;
                }
            }

            await next.Invoke(context);
        }
    }
}
