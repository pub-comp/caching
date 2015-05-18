using System.Runtime.Caching;
using PubComp.Caching.Core;

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
