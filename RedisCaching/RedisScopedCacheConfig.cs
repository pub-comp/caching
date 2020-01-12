using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.RedisCaching
{
    public class RedisScopedCacheConfig : CacheConfig
    {
        public RedisCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new RedisScopedCache(this.Name, this.Policy);
        }
    }
}
