using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheConfig : CacheConfig
    {
        public RedisCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new RedisCache(this.Name, this.Policy);
        }
    }
}
