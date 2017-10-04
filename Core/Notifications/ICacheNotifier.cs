using System;

namespace PubComp.Caching.Core.Notifications
{
    public interface ICacheNotifier
    {
        string Name { get; }

        void Subscribe(Func<CacheItemNotification, bool> callback);

        void UnSubscribe();

        void Publish(string cacheName, string key, CacheItemActionTypes action);
    }
}
