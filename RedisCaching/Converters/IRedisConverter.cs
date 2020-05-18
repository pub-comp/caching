using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.Converters
{
    internal interface IRedisConverter
    {
        string Type { get; } 

        RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem);
        CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString);
        RedisValue ToRedis(CacheItemNotification notification);
        CacheItemNotification FromRedis(RedisValue cacheNotificationString);

        RedisValue ToScopedRedis<TValue>(ScopedCacheItem<TValue> scopedCacheItem);
        ScopedCacheItem<TValue> FromScopedRedis<TValue>(RedisValue scopedCacheItemString);
    }
}
