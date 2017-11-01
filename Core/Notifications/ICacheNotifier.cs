using System;

namespace PubComp.Caching.Core.Notifications
{
    public interface ICacheNotifier
    {
        string Name { get; }

        void Subscribe(string cacheName, Func<CacheItemNotification, bool> callback);

        void UnSubscribe(string cacheName);

        void Publish(string cacheName, string key, CacheItemActionTypes action);
    }
}
