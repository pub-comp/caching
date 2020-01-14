using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface IScopedCache : ICache
    {
        CacheDirectivesOutcome SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);
        Task<CacheDirectivesOutcome> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);

        CacheDirectivesOutcome TryGetScoped<TValue>(String key, out TValue value);
        Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key);
    }
}
