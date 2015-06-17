using System;

namespace PubComp.Caching.SystemRuntime
{
    public class CacheItem
    {
        public Object Value { get; set; }

        public CacheItem()
        {
        }

        public CacheItem(Object value)
        {
            this.Value = value;
        }
    }
}
