using System.Runtime.Caching;
using PubComp.Caching.Core;

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
