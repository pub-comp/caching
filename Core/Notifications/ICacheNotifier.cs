using System;
using PubComp.Caching.Core.Events;

namespace PubComp.Caching.Core.Notifications
{
    public interface ICacheNotifier
    {
        string Name { get; }

        public bool IsInvalidateOnUpdateEnabled { get; }

        void Subscribe(string cacheName, Func<CacheItemNotification, bool> callback);

        void Subscribe(string cacheName, Func<CacheItemNotification, bool> cacheUpdatedCallback, EventHandler<ProviderStateChangedEventArgs> notifierProviderStateChangedCallback);

        void UnSubscribe(string cacheName);

        void Publish(string cacheName, string key, CacheItemActionTypes action);
    }
}
