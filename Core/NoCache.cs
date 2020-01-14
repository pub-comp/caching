using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public class NoCache : ICache
    {
        private readonly string name;

        public NoCache(string name)
        {
            this.name = name;
        }

        public NoCache() : this(string.Empty)
        {
        }

        public string Name
        {
            get { return name; }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            value = default;
            return false;
        }

        public Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return Task.FromResult(new TryGetResult<TValue> {WasFound = false});
        }

        public void Set<TValue>(string key, TValue value)
        {
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            return Task.FromResult<object>(null);
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            return getter();
        }

        public Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            return getter();
        }

        public void Clear(String key)
        {
        }

        public Task ClearAsync(string key)
        {
            return Task.FromResult<object>(null);
        }

        public void ClearAll()
        {
        }

        public Task ClearAllAsync()
        {
            return Task.FromResult<object>(null);
        }
    }
}