using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface ICache
    {
        string Name { get; }

        bool TryGet<TValue>(String key, out TValue value);

        Task<TryGetResult<TValue>> TryGetAsync<TValue>(String key);

        void Set<TValue>(String key, TValue value);

        Task SetAsync<TValue>(String key, TValue value);

        TValue Get<TValue>(String key, Func<TValue> getter);

        Task<TValue> GetAsync<TValue>(String key, Func<Task<TValue>> getter);

        void Clear(String key);

        Task ClearAsync(String key);

        void ClearAll();

        Task ClearAllAsync();
    }
}
