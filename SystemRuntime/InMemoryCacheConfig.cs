using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryCacheConfig : CacheConfig
    {
        public InMemoryPolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new InMemoryCache(this.Name, this.Policy);
        }
    }
}
