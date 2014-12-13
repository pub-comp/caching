using System;
using PubComp.Caching.Core;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockCache : ICache
    {
        private readonly string name;
        private readonly System.Runtime.Caching.CacheItemPolicy policy;

        public MockCache(String name, System.Runtime.Caching.CacheItemPolicy policy)
        {
            this.name = name;
            this.policy = policy;
        }

        public string Name
        {
            get { return this.name; }
        }

        public System.Runtime.Caching.CacheItemPolicy Policy
        {
            get { return this.policy; }
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            return getter();
        }

        public void Clear(string key)
        {
        }

        public void ClearAll()
        {
        }
    }
}
