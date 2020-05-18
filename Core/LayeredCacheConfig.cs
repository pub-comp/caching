using PubComp.Caching.Core.Config;

namespace PubComp.Caching.Core
{
    public class LayeredCacheConfig : CacheConfig
    {
        public LayeredCachePolicy Policy { get; set; }

        public override int LoadPriority { get; set; } = 2;

        public override ICache CreateCache()
        {
            return new LayeredCache(this.Name, this.Policy);
        }
    }
}
