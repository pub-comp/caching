using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemorySerializedCacheConfig : CacheConfig
    {
        public InMemoryPolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new InMemorySerializedCache(this.Name, this.Policy);
        }
    }
}
