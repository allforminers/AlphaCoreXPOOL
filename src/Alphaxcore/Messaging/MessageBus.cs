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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Alphaxcore.Util;
using NLog;

namespace Alphaxcore.Messaging
{
    /// <summary>
    ///     MessageBus represents an object that can act as a "Message Bus", a
    ///     simple way for ViewModels and other objects to communicate with each
    ///     other in a loosely coupled way.
    ///     Specifying which messages go where is done via a combination of the Type
    ///     of the message as well as an additional "Contract" parameter; this is a
    ///     unique string used to distinguish between messages of the same Type, and
    ///     is arbitrarily set by the client.
    /// </summary>
    public class MessageBus : IMessageBus
    {
        private readonly Dictionary<Tuple<Type, string>, NotAWeakReference> messageBus =
            new Dictionary<Tuple<Type, string>, NotAWeakReference>();

        private readonly IDictionary<Tuple<Type, string>, IScheduler> schedulerMappings =
            new Dictionary<Tuple<Type, string>, IScheduler>();

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Gets or sets the Current MessageBus.
        /// </summary>
        public static IMessageBus Current { get; set; } = new MessageBus();

        /// <summary>
        ///     Registers a scheduler for the type, which may be specified at runtime, and the contract.
        /// </summary>
        /// <remarks>
        ///     If a scheduler is already registered for the specified runtime and contract, this will overrwrite the existing
        ///     registration.
        /// </remarks>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="scheduler">
        ///     The scheduler on which to post the
        ///     notifications for the specified type and contract. CurrentThreadScheduler by default.
        /// </param>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        public void RegisterScheduler<T>(IScheduler scheduler, string contract = null)
        {
            schedulerMappings[new Tuple<Type, string>(typeof(T), contract)] = scheduler;
        }

        /// <summary>
        ///     Listen provides an Observable that will fire whenever a Message is
        ///     provided for this object via RegisterMessageSource or SendMessage.
        /// </summary>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        /// <returns>
        ///     An Observable representing the notifications posted to the
        ///     message bus.
        /// </returns>
        public IObservable<T> Listen<T>(string contract = null)
        {
            logger.Debug("Listening to {0}:{1}", typeof(T), contract);

            return setupSubjectIfNecessary<T>(contract).Skip(1);
        }

        /// <summary>
        ///     Listen provides an Observable that will fire whenever a Message is
        ///     provided for this object via RegisterMessageSource or SendMessage.
        /// </summary>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        /// <returns>
        ///     An Observable representing the notifications posted to the
        ///     message bus.
        /// </returns>
        public IObservable<T> ListenIncludeLatest<T>(string contract = null)
        {
            logger.Debug("Listening to {0}:{1}", typeof(T), contract);

            return setupSubjectIfNecessary<T>(contract);
        }

        /// <summary>
        ///     Determines if a particular message Type is registered.
        /// </summary>
        /// <param name="type">The Type of the message to listen to.</param>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        /// <returns>True if messages have been posted for this message Type.</returns>
        public bool IsRegistered(Type type, string contract = null)
        {
            var ret = false;
            withMessageBus(type, contract, (mb, tuple) => { ret = mb.ContainsKey(tuple) && mb[tuple].IsAlive; });

            return ret;
        }

        /// <summary>
        ///     Registers an Observable representing the stream of messages to send.
        ///     Another part of the code can then call Listen to retrieve this
        ///     Observable.
        /// </summary>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="source">
        ///     An Observable that will be subscribed to, and a
        ///     message sent out for each value provided.
        /// </param>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        public IDisposable RegisterMessageSource<T>(
            IObservable<T> source,
            string contract = null)
        {
            return source.Subscribe(setupSubjectIfNecessary<T>(contract));
        }

        /// <summary>
        ///     Sends a single message using the specified Type and contract.
        ///     Consider using RegisterMessageSource instead if you will be sending
        ///     messages in response to other changes such as property changes
        ///     or events.
        /// </summary>
        /// <typeparam name="T">The type of the message to send.</typeparam>
        /// <param name="message">The actual message to send</param>
        /// <param name="contract">
        ///     A unique string to distinguish messages with
        ///     identical types (i.e. "MyCoolViewModel") - if the message type is
        ///     only used for one purpose, leave this as null.
        /// </param>
        public void SendMessage<T>(T message, string contract = null)
        {
            setupSubjectIfNecessary<T>(contract).OnNext(message);
        }

        private ISubject<T> setupSubjectIfNecessary<T>(string contract)
        {
            ISubject<T> ret = null;

            withMessageBus(typeof(T), contract, (mb, tuple) =>
            {
                if(mb.TryGetValue(tuple, out var subjRef) && subjRef.IsAlive)
                {
                    ret = (ISubject<T>) subjRef.Target;
                    return;
                }

                ret = new ScheduledSubject<T>(getScheduler(tuple), null, new BehaviorSubject<T>(default(T)));
                mb[tuple] = new NotAWeakReference(ret);
            });

            return ret;
        }

        private void withMessageBus(
            Type type, string contract,
            Action<Dictionary<Tuple<Type, string>, NotAWeakReference>,
                Tuple<Type, string>> block)
        {
            lock(messageBus)
            {
                var tuple = new Tuple<Type, string>(type, contract);
                block(messageBus, tuple);
                if(messageBus.ContainsKey(tuple) && !messageBus[tuple].IsAlive)
                    messageBus.Remove(tuple);
            }
        }

        private IScheduler getScheduler(Tuple<Type, string> tuple)
        {
            schedulerMappings.TryGetValue(tuple, out var scheduler);
            return scheduler ?? CurrentThreadScheduler.Instance;
        }
    }

    internal class NotAWeakReference
    {
        public NotAWeakReference(object target)
        {
            Target = target;
        }

        public object Target { get; }
        public bool IsAlive => true;
    }
}

// vim: tw=120 ts=4 sw=4 et :
