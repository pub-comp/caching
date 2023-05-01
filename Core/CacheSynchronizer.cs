using PubComp.Caching.Core.Notifications;
using System;

namespace PubComp.Caching.Core
{
    public class CacheSynchronizer
    {
        private readonly ICache cache;
        private readonly ICacheNotifier notifier;
        private readonly bool invalidateOnStateChange;

        public bool IsActive { get; private set; }

        public CacheSynchronizer(ICache cache, ICacheNotifier notifier, bool invalidateOnStateChange)
        {
            this.cache = cache;
            this.notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            this.invalidateOnStateChange = invalidateOnStateChange;

            notifier.Subscribe(cache.Name, OnCacheUpdated, OnNotifierStateChanged);
        }

        public CacheSynchronizer(ICache cache, ICacheNotifier notifier) 
            : this(cache, notifier, invalidateOnStateChange: false)
        {
        }

        private void OnNotifierStateChanged(object sender, Events.ProviderStateChangedEventArgs args)
        {
            var oldState = IsActive;
            IsActive = args.IsAvailable;

            if (this.invalidateOnStateChange && oldState != args.IsAvailable)
                OnCacheUpdated(new CacheItemNotification("self", this.cache.Name, null, CacheItemActionTypes.RemoveAll));
        }

        private bool OnCacheUpdated(CacheItemNotification notification)
        {
            if (!this.cache.Name.Equals(notification.CacheName, StringComparison.InvariantCultureIgnoreCase))
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
            => CreateCacheSynchronizer(cache, syncProviderName, invalidateOnStateChange: false);

        public static CacheSynchronizer CreateCacheSynchronizer(ICache cache, string syncProviderName, bool invalidateOnStateChange)
        {
            if (string.IsNullOrEmpty(syncProviderName))
                return null;

            var notifier = CacheManager.GetNotifier(syncProviderName);

            if (notifier == null)
                return null;

            var synchronizer = new CacheSynchronizer(cache, notifier, invalidateOnStateChange);

            CacheManager.Associate(cache, notifier);

            return synchronizer;
        }
    }
}
