using System;

namespace PubComp.Caching.RedisCaching
{
    public class ScopedCacheItem<TValue> : Core.ScopedValue<TValue>
    {
        private const string CacheNamePrefix = "sc=";
        private const string KeyPrefix = ":k=";

        public String Id { get; set; }

        public TimeSpan? ExpireIn { get; set; }

        public ScopedCacheItem()
        {
        }

        public static String GetId(String cacheName, String key)
        {
            return string.Concat(CacheNamePrefix, cacheName, KeyPrefix, key);
        }

        public ScopedCacheItem(String cacheName, String key, TValue value, DateTimeOffset valueTimestamp)
        {
            this.Id = GetId(cacheName, key);
            this.Value = value;
            this.ValueTimestamp = valueTimestamp;
        }

        public ScopedCacheItem(String cacheName, String key, TValue value, DateTimeOffset valueTimestamp, TimeSpan? expireIn)
        {
            this.Id = GetId(cacheName, key);
            this.Value = value;
            this.ValueTimestamp = valueTimestamp;
            this.ExpireIn = expireIn;
        }

    }
}
