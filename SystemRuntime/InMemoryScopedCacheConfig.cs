using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryScopedCacheConfig : CacheConfig
    {
        public InMemoryPolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new InMemoryScopedCache(this.Name, this.Policy);
        }
    }
}
