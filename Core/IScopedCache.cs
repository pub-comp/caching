using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface IScopedCache : ICache
    {
        bool IsActive { get; }

        CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);
        Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp);

        GetScopedResult<TValue> GetScoped<TValue>(string key, Func<ScopedValue<TValue>> getter);
        Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedValue<TValue>>> getter);

        CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedValue<TValue> scopedValue);
        Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key);
    }
}
