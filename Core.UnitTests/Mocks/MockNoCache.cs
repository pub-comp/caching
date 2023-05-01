using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockNoCache : ICache
    {
        private readonly string name;
        private readonly MockCachePolicy policy;

        public MockNoCache(String name, MockCachePolicy policy)
        {
            this.name = name;
            this.policy = policy;
        }

        public string Name
        {
            get { return this.name; }
        }

        public MockCachePolicy Policy
        {
            get { return this.policy; }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            value = default;
            return false;
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return new TryGetResult<TValue> {WasFound = false, Value = default};
        }

        public void Set<TValue>(string key, TValue value)
        {
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            return Task.FromResult<object>(null);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            return getter();
        }

        public Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            return getter();
        }

        public void Clear(string key)
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
