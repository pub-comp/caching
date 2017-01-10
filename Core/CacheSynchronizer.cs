using System;

namespace PubComp.Caching.Core
{
    public class CacheSynchronizer
    {
        private readonly ICache cache;
        private readonly ICacheNotifier notifications;

        public CacheSynchronizer(ICache cache, ICacheNotifier notifications)
        {
            this.cache = cache;
            this.notifications = notifications;

            this.notifications.Subscribe(CacheUpdated);
        }
        
        private bool CacheUpdated(CacheItemNotification notification)
        {
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

        public static CacheSynchronizer CreateCacheSynchronizer(ICache cache, string autoSyncProviderName)
        {
            if (!String.IsNullOrEmpty(autoSyncProviderName))
            {
                var notifications = CacheManager.GetCacheNotificationsProvider(cache.Name, autoSyncProviderName);

                if (notifications != null)
                {
                    var synchronizer = new CacheSynchronizer(cache, notifications);
                    return synchronizer;
                }
            }
            return null;
        }
    }
}
