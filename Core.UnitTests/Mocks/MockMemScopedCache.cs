using PubComp.Caching.SystemRuntime;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockMemScopedCache : IScopedCache, IMockCache
    {
        private readonly MockScopedCacheInner innerCache;

        public bool IsActive { get; set; } = true;
        public object GetDetails() => new { InnerCachePolicy = innerCache?.GetDetails() };

        public MockMemScopedCache(string name)
        {
            this.innerCache = new MockScopedCacheInner(name);
        }

        public int Hits { get { return this.innerCache.Hits; } }

        public int Misses { get { return this.innerCache.Misses; } }

        public string Name { get { return this.innerCache.Name; } }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return this.innerCache.TryGet(key, out value);
        }

        public Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return this.innerCache.TryGetAsync<TValue>(key);
        }

        public void Set<TValue>(string key, TValue value)
        {
            this.innerCache.Set(key, value);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            return this.innerCache.SetAsync(key, value);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            return this.innerCache.Get(key, getter);
        }

        public Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            return this.innerCache.GetAsync(key, getter);
        }

        public CacheMethodTaken SetScoped<TValue>(string key, TValue value, DateTimeOffset valueTimestamp)
        {
            return innerCache.SetScoped(key, value, valueTimestamp);
        }

        public Task<CacheMethodTaken> SetScopedAsync<TValue>(string key, TValue value, DateTimeOffset valueTimestamp)
        {
            return innerCache.SetScopedAsync(key, value, valueTimestamp);
        }

        public GetScopedResult<TValue> GetScoped<TValue>(string key, Func<ScopedValue<TValue>> getter)
        {
            return innerCache.GetScoped(key, getter);
        }

        public Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedValue<TValue>>> getter)
        {
            return innerCache.GetScopedAsync(key, getter);
        }

        public CacheMethodTaken TryGetScoped<TValue>(string key, out ScopedValue<TValue> scopedValue)
        {
            return innerCache.TryGetScoped(key, out scopedValue);
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(string key)
        {
            return innerCache.TryGetScopedAsync<TValue>(key);
        }

        public void Clear(string key)
        {
            this.innerCache.Clear(key);
        }

        public Task ClearAsync(string key)
        {
            Clear(key);
            return Task.CompletedTask;
        }

        public void ClearAll()
        {
            this.innerCache.ClearAll();
            this.innerCache.ResetCounters();
        }

        public Task ClearAllAsync()
        {
            ClearAll();
            return Task.CompletedTask;
        }

        public void ClearAll(bool doResetCounters)
        {
            this.innerCache.ClearAll();

            if (doResetCounters)
                this.innerCache.ResetCounters();
        }

        public class MockScopedCacheInner : InMemoryScopedCache
        {
            private int hits;
            private int misses;

            public MockScopedCacheInner(string name)
                : base(name,
                    new InMemoryPolicy
                    {
                        SlidingExpiration = new TimeSpan(0, 2, 0),
                    })
            {
            }

            protected override CacheMethodTaken TryGetScopedInner<TValue>(string key, out ScopedValue<TValue> value)
            {
                var cacheMethodTaken = base.TryGetScopedInner(key, out value);
                if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get)) hits++;
                if (cacheMethodTaken.HasFlag(CacheMethodTaken.GetMiss)) misses++;
                return cacheMethodTaken;
            }

            public int Hits { get { return this.hits; } }

            public int Misses { get { return this.misses; } }

            internal void ResetCounters()
            {
                this.hits = 0;
                this.misses = 0;
            }
        }
    }
}
