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
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Alphaxcore.Configuration;
using Alphaxcore.Contracts;
using Alphaxcore.Messaging;
using Alphaxcore.Notifications.Messages;
using Newtonsoft.Json;
using NLog;

namespace Alphaxcore.Notifications
{
    public class NotificationService
    {
        public NotificationService(
            ClusterConfig clusterConfig,
            JsonSerializerSettings serializerSettings,
            IMessageBus messageBus)
        {
            Contract.RequiresNonNull(clusterConfig, nameof(clusterConfig));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.clusterConfig = clusterConfig;
            this.serializerSettings = serializerSettings;

            poolConfigs = clusterConfig.Pools.ToDictionary(x => x.Id, x => x);

            adminEmail = clusterConfig.Notifications?.Admin?.EmailAddress;
            //adminPhone = null;

            if(clusterConfig.Notifications?.Enabled == true)
            {
                queue = new BlockingCollection<QueuedNotification>();

                queueSub = queue.GetConsumingEnumerable()
                    .ToObservable(TaskPoolScheduler.Default)
                    .Select(notification => Observable.FromAsync(() => SendNotificationAsync(notification)))
                    .Concat()
                    .Subscribe();

                messageBus.Listen<AdminNotification>()
                    .Subscribe(x =>
                    {
                        queue?.Add(new QueuedNotification
                        {
                            Category = NotificationCategory.Admin,
                            Subject = x.Subject,
                            Msg = x.Message
                        });
                    });

                messageBus.Listen<BlockFoundNotification>()
                    .Subscribe(x =>
                    {
                        queue?.Add(new QueuedNotification
                        {
                            Category = NotificationCategory.Block,
                            PoolId = x.PoolId,
                            Subject = "Block Notification",
                            Msg = $"Pool {x.PoolId} found block candidate {x.BlockHeight}"
                        });
                    });

                messageBus.Listen<PaymentNotification>()
                    .Subscribe(x =>
                    {
                        if(string.IsNullOrEmpty(x.Error))
                        {
                            var coin = poolConfigs[x.PoolId].Template;

                            // prepare tx links
                            string[] txLinks = null;

                            if(!string.IsNullOrEmpty(coin.ExplorerTxLink))
                                txLinks = x.TxIds.Select(txHash => string.Format(coin.ExplorerTxLink, txHash)).ToArray();

                            queue?.Add(new QueuedNotification
                            {
                                Category = NotificationCategory.PaymentSuccess,
                                PoolId = x.PoolId,
                                Subject = "Payout Success Notification",
                                Msg = $"Paid {FormatAmount(x.Amount, x.PoolId)} from pool {x.PoolId} to {x.RecpientsCount} recipients in Transaction(s) {txLinks}."
                            });
                        }

                        else
                        {
                            queue?.Add(new QueuedNotification
                            {
                                Category = NotificationCategory.PaymentFailure,
                                PoolId = x.PoolId,
                                Subject = "Payout Failure Notification",
                                Msg = $"Failed to pay out {x.Amount} {poolConfigs[x.PoolId].Template.Symbol} from pool {x.PoolId}: {x.Error}"
                            });
                        }
                    });
            }
        }

        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly ClusterConfig clusterConfig;
        private readonly JsonSerializerSettings serializerSettings;
        private readonly Dictionary<string, PoolConfig> poolConfigs;

        private readonly string adminEmail;

        //private readonly string adminPhone;
        private readonly BlockingCollection<QueuedNotification> queue;
        private IDisposable queueSub;

        enum NotificationCategory
        {
            Admin,
            Block,
            PaymentSuccess,
            PaymentFailure,
        }

        struct QueuedNotification
        {
            public NotificationCategory Category;
            public string PoolId;
            public string Subject;
            public string Msg;
        }

        public string FormatAmount(decimal amount, string poolId)
        {
            return $"{amount:0.#####} {poolConfigs[poolId].Template.Symbol}";
        }

        private async Task SendNotificationAsync(QueuedNotification notification)
        {
            logger.Debug(() => $"SendNotificationAsync");

            try
            {
                var poolConfig = !string.IsNullOrEmpty(notification.PoolId) ? poolConfigs[notification.PoolId] : null;

                switch(notification.Category)
                {
                    case NotificationCategory.Admin:
                        if(clusterConfig.Notifications?.Admin?.Enabled == true)
                            await SendEmailAsync(adminEmail, notification.Subject, notification.Msg);
                        break;

                    case NotificationCategory.Block:
                        if(clusterConfig.Notifications?.Admin?.Enabled == true &&
                            clusterConfig.Notifications?.Admin?.NotifyBlockFound == true)
                            await SendEmailAsync(adminEmail, notification.Subject, notification.Msg);
                        break;

                    case NotificationCategory.PaymentSuccess:
                    case NotificationCategory.PaymentFailure:
                        if(clusterConfig.Notifications?.Admin?.Enabled == true &&
                            clusterConfig.Notifications?.Admin?.NotifyPaymentSuccess == true)
                            await SendEmailAsync(adminEmail, notification.Subject, notification.Msg);
                        break;
                }
            }

            catch(Exception ex)
            {
                logger.Error(ex, $"Error sending notification");
            }
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            logger.Info(() => $"Sending '{subject.ToLower()}' email to {recipient}");

            var emailSenderConfig = clusterConfig.Notifications.Email;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSenderConfig.FromName, emailSenderConfig.FromAddress));
            message.To.Add(new MailboxAddress("", recipient));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using(var client = new SmtpClient())
            {
                await client.ConnectAsync(emailSenderConfig.Host, emailSenderConfig.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSenderConfig.User, emailSenderConfig.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            logger.Info(() => $"Sent '{subject.ToLower()}' email to {recipient}");
        }
    }
}
