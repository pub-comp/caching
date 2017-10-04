using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifierConfig : NotifierConfig
    {
        public RedisCacheNotifierPolicy Policy { get; set; }

        public override ICacheNotifier CreateCacheNotifier()
        {
            return new RedisCacheNotifier(this.Name, this.Policy);
        }
    }
}
