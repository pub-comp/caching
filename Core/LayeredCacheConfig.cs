using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PubComp.Caching.Core
{
    public class LayeredCacheConfig : CacheConfig
    {
        public LayeredCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new LayeredCache(this.Name, this.Policy.Level1CacheName, this.Policy.Level2CacheName);
        }
    }
}
