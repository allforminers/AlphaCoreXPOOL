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
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Alphaxcore.Util
{
    public class ScheduledSubject<T> : ISubject<T>
    {
        public ScheduledSubject(IScheduler scheduler, IObserver<T> defaultObserver = null, ISubject<T> defaultSubject = null)
        {
            _scheduler = scheduler;
            _defaultObserver = defaultObserver;
            _subject = defaultSubject ?? new Subject<T>();

            if(defaultObserver != null)
                _defaultObserverSub = _subject.ObserveOn(_scheduler).Subscribe(_defaultObserver);
        }

        private readonly IObserver<T> _defaultObserver;
        private readonly IScheduler _scheduler;
        private readonly ISubject<T> _subject;
        private IDisposable _defaultObserverSub = Disposable.Empty;

        private int _observerRefCount;

        public void OnCompleted()
        {
            _subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _subject.OnError(error);
        }

        public void OnNext(T value)
        {
            _subject.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Interlocked.Exchange(ref _defaultObserverSub, Disposable.Empty).Dispose();

            Interlocked.Increment(ref _observerRefCount);

            return new CompositeDisposable(
                _subject.ObserveOn(_scheduler).Subscribe(observer),
                Disposable.Create(() =>
                {
                    if(Interlocked.Decrement(ref _observerRefCount) <= 0 && _defaultObserver != null)
                        _defaultObserverSub = _subject.ObserveOn(_scheduler).Subscribe(_defaultObserver);
                }));
        }

        public void Dispose()
        {
            if(_subject is IDisposable)
                ((IDisposable) _subject).Dispose();
        }
    }
}
