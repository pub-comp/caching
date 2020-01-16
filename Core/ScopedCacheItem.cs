using System;

namespace PubComp.Caching.Core
{
    public class ScopedCacheItem<TValue>
    {
        public TValue Value { get; set; }
        public DateTimeOffset ValueTimestamp { get; set; }

        public ScopedCacheItem()
        {
        }

        public ScopedCacheItem(TValue value, DateTimeOffset valueTimestamp)
        {
            this.Value = value;
            this.ValueTimestamp = valueTimestamp;
        }
    }
}