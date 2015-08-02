using PubComp.Caching.Core;

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
