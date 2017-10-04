using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.MongoDbCaching
{
    public class MongoDbCacheConfig : CacheConfig
    {
        public MongoDbCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new MongoDbCache(this.Name, this.Policy);
        }
    }
}
