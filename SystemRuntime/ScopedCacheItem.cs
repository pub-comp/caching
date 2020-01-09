using System;

namespace PubComp.Caching.SystemRuntime
{
    public class ScopedCacheItem
    {
        public Object Value { get; set; }

        public DateTimeOffset ValueTimestamp { get; set; }

        public ScopedCacheItem()
        {
        }

        public ScopedCacheItem(Object value, DateTimeOffset valueTimestamp)
        {
            this.Value = value;
            this.ValueTimestamp = valueTimestamp;
        }
    }
}
