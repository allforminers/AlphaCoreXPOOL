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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Alphaxcore.Util;
using NLog;
using ZeroMQ;
using ZeroMQ.Monitoring;

namespace Alphaxcore.Extensions
{
    public static class ZmqExtensions
    {
        private static readonly ConcurrentDictionary<string, (byte[] PubKey, byte[] SecretKey)> knownKeys =
            new ConcurrentDictionary<string, (byte[] PubKey, byte[] SecretKey)>();

        private static readonly Lazy<(byte[] PubKey, byte[] SecretKey)> ownKey = new Lazy<(byte[] PubKey, byte[] SecretKey)>(() =>
        {
            if(!ZContext.Has("curve"))
                throw new NotSupportedException("ZMQ library does not support curve");

            Z85.CurveKeypair(out var pubKey, out var secretKey);
            return (pubKey, secretKey);
        });

        const int PasswordIterations = 5000;
        private static readonly byte[] NoSalt = new byte[32];

        private static byte[] DeriveKey(string password, int length = 32)
        {
            using(var kbd = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), NoSalt, PasswordIterations))
            {
                var block = kbd.GetBytes(length);
                return block;
            }
        }

        private static long monitorSocketIndex = 0;

        public static IObservable<ZMonitorEventArgs> MonitorAsObservable(this ZSocket socket)
        {
            return Observable.Defer(() => Observable.Create<ZMonitorEventArgs>(obs =>
            {
                var url = $"inproc://monitor{Interlocked.Increment(ref monitorSocketIndex)}";
                var monitor = ZMonitor.Create(socket.Context, url);
                var cts = new CancellationTokenSource();

                void OnEvent(object sender, ZMonitorEventArgs e)
                {
                    obs.OnNext(e);
                }

                monitor.AllEvents += OnEvent;

                socket.Monitor(url);
                monitor.Start(cts);

                return Disposable.Create(() =>
                {
                    using(new CompositeDisposable(monitor, cts))
                    {
                        monitor.AllEvents -= OnEvent;
                        monitor.Stop();
                    }
                });
            }));
        }

        public static void LogMonitorEvent(ILogger logger, ZMonitorEventArgs e)
        {
            logger.Info(() => $"[ZMQ] [{e.Event.Address}] {Enum.GetName(typeof(ZMonitorEvents), e.Event.Event)} [{e.Event.EventValue}]");
        }

        /// <summary>
        /// Sets up server-side socket to utilize ZeroMQ Curve Transport-Layer Security
        /// </summary>
        public static void SetupCurveTlsServer(this ZSocket socket, string keyPlain, ILogger logger)
        {
            keyPlain = keyPlain?.Trim();

            if(string.IsNullOrEmpty(keyPlain))
                return;

            if(!ZContext.Has("curve"))
                logger.ThrowLogPoolStartupException("Unable to initialize ZMQ Curve Transport-Layer-Security. Your ZMQ library was compiled without Curve support!");

            // Get server's public key
            byte[] keyBytes = null;
            byte[] serverPubKey = null;

            if(!knownKeys.TryGetValue(keyPlain, out var serverKeys))
            {
                keyBytes = DeriveKey(keyPlain, 32);

                // Derive server's public-key from shared secret
                Z85.CurvePublic(out serverPubKey, keyBytes.ToZ85Encoded());
                knownKeys[keyPlain] = (serverPubKey, keyBytes);
            }

            else
            {
                keyBytes = serverKeys.SecretKey;
                serverPubKey = serverKeys.PubKey;
            }

            // set socket options
            socket.CurveServer = true;
            socket.CurveSecretKey = keyBytes;
            socket.CurvePublicKey = serverPubKey;
        }

        /// <summary>
        /// Sets up client-side socket to utilize ZeroMQ Curve Transport-Layer Security
        /// </summary>
        public static void SetupCurveTlsClient(this ZSocket socket, string keyPlain, ILogger logger)
        {
            keyPlain = keyPlain?.Trim();

            if(string.IsNullOrEmpty(keyPlain))
                return;

            if(!ZContext.Has("curve"))
                logger.ThrowLogPoolStartupException("Unable to initialize ZMQ Curve Transport-Layer-Security. Your ZMQ library was compiled without Curve support!");

            // Get server's public key
            byte[] serverPubKey = null;

            if(!knownKeys.TryGetValue(keyPlain, out var serverKeys))
            {
                var keyBytes = DeriveKey(keyPlain, 32);

                // Derive server's public-key from shared secret
                Z85.CurvePublic(out serverPubKey, keyBytes.ToZ85Encoded());
                knownKeys[keyPlain] = (serverPubKey, keyBytes);
            }

            else
                serverPubKey = serverKeys.PubKey;

            // set socket options
            socket.CurveServer = false;
            socket.CurveServerKey = serverPubKey;
            socket.CurveSecretKey = ownKey.Value.SecretKey;
            socket.CurvePublicKey = ownKey.Value.PubKey;
        }
    }
}
