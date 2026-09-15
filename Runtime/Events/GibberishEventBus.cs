using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGibberish
{
    public sealed class GibberishEventBus : IGibberishEventBus
    {
        private readonly object _gate = new object();
        private readonly Dictionary<Type, IEventChannel> _channels = new Dictionary<Type, IEventChannel>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_gate)
            {
                var channel = GetOrCreateChannel<TEvent>();
                channel.Add(handler);
            }

            return new Subscription<TEvent>(this, handler);
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            Action<TEvent>[] handlers;
            lock (_gate)
            {
                if (!_channels.TryGetValue(typeof(TEvent), out var rawChannel))
                {
                    return;
                }

                handlers = ((EventChannel<TEvent>)rawChannel).Snapshot();
            }

            for (var index = 0; index < handlers.Length; index++)
            {
                try
                {
                    handlers[index].Invoke(eventData);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private EventChannel<TEvent> GetOrCreateChannel<TEvent>()
        {
            var type = typeof(TEvent);
            if (_channels.TryGetValue(type, out var rawChannel))
            {
                return (EventChannel<TEvent>)rawChannel;
            }

            var channel = new EventChannel<TEvent>();
            _channels.Add(type, channel);
            return channel;
        }

        private void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            lock (_gate)
            {
                if (!_channels.TryGetValue(typeof(TEvent), out var rawChannel))
                {
                    return;
                }

                var channel = (EventChannel<TEvent>)rawChannel;
                channel.Remove(handler);
                if (channel.Count == 0)
                {
                    _channels.Remove(typeof(TEvent));
                }
            }
        }

        private interface IEventChannel
        {
        }

        private sealed class EventChannel<TEvent> : IEventChannel
        {
            private readonly List<Action<TEvent>> _handlers = new List<Action<TEvent>>();

            public int Count => _handlers.Count;

            public void Add(Action<TEvent> handler)
            {
                _handlers.Add(handler);
            }

            public void Remove(Action<TEvent> handler)
            {
                _handlers.Remove(handler);
            }

            public Action<TEvent>[] Snapshot()
            {
                return _handlers.ToArray();
            }
        }

        private sealed class Subscription<TEvent> : IDisposable
        {
            private GibberishEventBus _owner;
            private Action<TEvent> _handler;

            public Subscription(GibberishEventBus owner, Action<TEvent> handler)
            {
                _owner = owner;
                _handler = handler;
            }

            public void Dispose()
            {
                var owner = _owner;
                var handler = _handler;
                if (owner == null || handler == null)
                {
                    return;
                }

                _owner = null;
                _handler = null;
                owner.Unsubscribe(handler);
            }
        }
    }
}
