using PubComp.Caching.Core.Notifications;
using System;

namespace PubComp.Caching.Core
{
    public class CacheSynchronizer
    {
        private readonly ICache cache;
        private readonly ICacheNotifier notifier;

        public bool IsActive { get; private set; }
        public bool IsInvalidateOnUpdateEnabled => notifier.IsInvalidateOnUpdateEnabled;
        
        public CacheSynchronizer(ICache cache, ICacheNotifier notifier)
        {
            this.cache = cache;
            this.notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));

            notifier.Subscribe(cache.Name, OnCacheUpdated, OnNotifierStateChanged);
        }

        public bool TryPublishCacheItemUpdated(string key)
            => notifier.TryPublish(cache.Name, key, CacheItemActionTypes.Updated);

        private void OnNotifierStateChanged(object sender, Events.ProviderStateChangedEventArgs args)
        {
            IsActive = args.NewState;
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
