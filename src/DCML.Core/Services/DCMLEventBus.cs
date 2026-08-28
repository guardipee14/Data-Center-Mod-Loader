using System;
using System.Collections.Generic;
using DCML.Core.Abstractions;

namespace DCML.Core.Services;

public sealed class DCMLEventBus : IDCMLEventBus
{
    private readonly object _syncRoot = new object();

    private readonly Dictionary<Type, List<Subscription>> _subscriptions =
        new Dictionary<Type, List<Subscription>>();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var subscription =
            new Subscription(
                typeof(TEvent),
                value => handler((TEvent)value),
                RemoveSubscription);

        lock (_syncRoot)
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var subscribers))
            {
                subscribers = new List<Subscription>();
                _subscriptions[typeof(TEvent)] = subscribers;
            }

            subscribers.Add(subscription);
        }

        return subscription;
    }

    public void Publish<TEvent>(TEvent eventData)
    {
        if (eventData is null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        Subscription[] subscribers;

        lock (_syncRoot)
        {
            if (
                !_subscriptions.TryGetValue(typeof(TEvent), out var registered) ||
                registered.Count == 0)
            {
                return;
            }

            subscribers = registered.ToArray();
        }

        List<Exception>? failures = null;

        foreach (var subscriber in subscribers)
        {
            if (subscriber.IsDisposed)
            {
                continue;
            }

            try
            {
                subscriber.Invoke(eventData!);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }
        }

        if (failures is not null && failures.Count != 0)
        {
            throw new AggregateException(
                "One or more DCML event subscribers failed.",
                failures);
        }
    }

    private void RemoveSubscription(Subscription subscription)
    {
        lock (_syncRoot)
        {
            if (
                !_subscriptions.TryGetValue(
                    subscription.EventType,
                    out var subscribers))
            {
                return;
            }

            subscribers.Remove(subscription);

            if (subscribers.Count == 0)
            {
                _subscriptions.Remove(subscription.EventType);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action<object> _handler;

        private readonly Action<Subscription> _remove;

        private bool _disposed;

        public Subscription(
            Type eventType,
            Action<object> handler,
            Action<Subscription> remove)
        {
            EventType = eventType;
            _handler = handler;
            _remove = remove;
        }

        public Type EventType { get; }

        public bool IsDisposed
        {
            get
            {
                lock (this)
                {
                    return _disposed;
                }
            }
        }

        public void Invoke(object value)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }
            }

            _handler(value);
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            _remove(this);
        }
    }
}
