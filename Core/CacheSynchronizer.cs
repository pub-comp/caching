using System;
using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.Core
{
    public class CacheSynchronizer
    {
        private readonly ICache cache;
        private readonly ICacheNotifier notifier;

        public bool IsActive { get => this.notifier.IsActive }

        public CacheSynchronizer(ICache cache, ICacheNotifier notifier)
        {
            this.cache = cache;
            this.notifier = notifier;

            notifier.Subscribe(cache.Name, OnCacheUpdated, OnNotifierStateChanged);
        }

        private void OnNotifierStateChanged(bool newState)
        {
            OnCacheUpdated(new CacheItemNotification("self", this.cache.Name, null, CacheItemActionTypes.RemoveAll));
        }

        private bool OnCacheUpdated(CacheItemNotification notification)
        {
            if (notification.CacheName != this.cache.Name)
                return false;

            System.Diagnostics.Debug.WriteLine("Incoming Notification::From:{0}, Cache:{1}, Key:{2}, Action:{3}",
                notification.Sender,
                notification.CacheName,
                String.IsNullOrEmpty(notification.Key) ? "undefined" : notification.Key,
                notification.Action.ToString());

            if (notification.Action == CacheItemActionTypes.RemoveAll)
            {
                cache.ClearAll();
            }
            else
            {
                cache.Clear(notification.Key);
            }

            return true;
        }

        public static CacheSynchronizer CreateCacheSynchronizer(ICache cache, string syncProviderName)
        {
            if (string.IsNullOrEmpty(syncProviderName))
                return null;

            var notifier = CacheManager.GetNotifier(syncProviderName);

            if (notifier == null)
                return null;

            var synchronizer = new CacheSynchronizer(cache, notifier);

            CacheManager.Associate(cache, notifier);

            return synchronizer;
        }
    }
}
