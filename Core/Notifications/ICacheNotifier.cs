using PubComp.Caching.Core.Events;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.Notifications
{
    public interface ICacheNotifier
    {
        string Name { get; }

        void Subscribe(string cacheName, Func<CacheItemNotification, bool> callback);
        void Subscribe(string cacheName, Func<CacheItemNotification, bool> cacheUpdatedCallback, EventHandler<ProviderStateChangedEventArgs> notifierProviderStateChangedCallback);

        void UnSubscribe(string cacheName);

        void Publish(string cacheName, string key, CacheItemActionTypes action);
        Task PublishAsync(string cacheName, string key, CacheItemActionTypes action);

        bool TryPublish(string cacheName, string key, CacheItemActionTypes action);
        Task<bool> TryPublishAsync(string cacheName, string key, CacheItemActionTypes action);
    }
}
