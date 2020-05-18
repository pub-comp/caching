using PubComp.Caching.Core.Config;

namespace PubComp.Caching.Core
{
    public class LayeredScopedCacheConfig : CacheConfig
    {
        public LayeredScopedCachePolicy Policy { get; set; }

        public override int LoadPriority { get; set; } = 2;

        public override ICache CreateCache()
        {
            return new LayeredScopedCache(this.Name, this.Policy);
        }
    }
}
