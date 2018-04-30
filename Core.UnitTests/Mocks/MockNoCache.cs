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
            value = default(TValue);
            return false;
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return new TryGetResult<TValue> {WasFound = false, Value = default(TValue)};
        }

        public void Set<TValue>(string key, TValue value)
        {
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            return getter();
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            return await getter();
        }

        public void Clear(string key)
        {
        }

        public void ClearAll()
        {
        }
    }
}
