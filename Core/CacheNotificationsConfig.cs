namespace PubComp.Caching.Core
{
    public abstract class CacheNotificationsConfig
    {
        public ConfigAction Action { get; set; }

        public string Name { get; set; }

        public abstract ICacheNotifier CreateCacheNotifications(string cacheName);
    }
}
