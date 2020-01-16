using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface IScopedCache : ICache
    {
        CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);
        Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);

        ScopedCacheItem<TValue> GetScoped<TValue>(string key, Func<ScopedCacheItem<TValue>> getter);
        Task<ScopedCacheItem<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedCacheItem<TValue>>> getter);

        CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedCacheItem<TValue> value);
        Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key);
    }
}
