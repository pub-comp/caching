using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface IScopedCache
    {
        CacheDirectivesOutcome SetScoped<TValue>(string key, TValue value, DateTimeOffset valueTimestamp);
        Task<CacheDirectivesOutcome> SetScopedAsync<TValue>(string key, TValue value, DateTimeOffset valueTimestamp);

        //CacheDirectivesOutcome GetScoped<TValue>(String key, Func<> getter, out TValue value);
        //Task<GetScopedResult<TValue>> GetAsync<TValue>(String key, Func<Task<TValue>> getter);

        CacheDirectivesOutcome TryGetScoped<TValue>(string key, out TValue value);
        Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(string key);

        void Clear(String key);
        Task ClearAsync(String key);

        void ClearAll();
        Task ClearAllAsync();
    }
}
