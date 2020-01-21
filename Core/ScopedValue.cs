using System;

namespace PubComp.Caching.Core
{
    public class ScopedValue<TValue>
    {
        public TValue Value { get; set; }
        public DateTimeOffset ValueTimestamp { get; set; }

        public ScopedValue()
        {
        }

        public ScopedValue(TValue value, DateTimeOffset valueTimestamp)
        {
            this.Value = value;
            this.ValueTimestamp = valueTimestamp;
        }
    }
}