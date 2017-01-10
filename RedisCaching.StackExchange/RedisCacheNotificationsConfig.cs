using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisCacheNotificationsConfig : CacheNotificationsConfig
    {
        public RedisCacheNotificationsPolicy Policy { get; set; }

        public override ICacheNotifier CreateCacheNotifications(string cachename)
        {
            return new RedisCacheNotifications(cachename, this.Policy);
        }
    }
}
