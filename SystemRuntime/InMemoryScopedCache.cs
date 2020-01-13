using PubComp.Caching.Core;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryScopedCache : ObjectCache, IScopedCache
    {
        public InMemoryScopedCache(String name, InMemoryPolicy policy)
            : base(name, new System.Runtime.Caching.MemoryCache(name), policy)
        {
        }

        public InMemoryScopedCache(String name, TimeSpan slidingExpiration)
            : this(name,
                new InMemoryPolicy
                {
                    SlidingExpiration = slidingExpiration
                })
        {
        }

        public InMemoryScopedCache(String name, DateTimeOffset absoluteExpiration)
            : this(name,
                new InMemoryPolicy
                {
                    AbsoluteExpiration = absoluteExpiration
                })
        {
        }

        protected override void Add<TValue>(string key, TValue value)
        {
            var valueTimestamp = ScopedContext<CacheDirectives>.CurrentTimestamp;
            AddScoped(key, value, valueTimestamp);
        }

        private CacheDirectivesOutcome AddScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                InnerCache.Set(key, new ScopedCacheItem(value, valueTimestamp), GetRuntimePolicy(), regionName: null);
                return new CacheDirectivesOutcome(CacheMethodTaken.Set, valueTimestamp);
            }

            return new CacheDirectivesOutcome(CacheMethodTaken.None);
        }

        public CacheDirectivesOutcome SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            return AddScoped(key, value, valueTimestamp);
        }

        public Task<CacheDirectivesOutcome> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var outcome = SetScoped(key, value, valueTimestamp);
            return Task.FromResult(outcome);
        }

        protected override bool TryGetInner<TValue>(string key, out TValue value)
        {
            var outcome = TryGetScopedInner(key, out value);
            return outcome.MethodTaken.HasFlag(CacheMethodTaken.Get);
        }

        private CacheDirectivesOutcome TryGetScopedInner<TValue>(string key, out TValue value)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default(TValue);
                return new CacheDirectivesOutcome(CacheMethodTaken.None);
            }

            if (InnerCache.Get(key, regionName: null) is ScopedCacheItem item
                && item.ValueTimestamp > directives.MinimumValueTimestamp)
            {
                value = item.Value is TValue itemValue ? itemValue : default;
                return new CacheDirectivesOutcome(CacheMethodTaken.Get);
            }

            value = default;
            return new CacheDirectivesOutcome(CacheMethodTaken.GetMiss);
        }

        public CacheDirectivesOutcome TryGetScoped<TValue>(String key, out TValue value)
        {
            return TryGetScopedInner(key, out value);
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key)
        {
            return Task.FromResult(new TryGetScopedResult<TValue>
            {
                Outcome = TryGetScopedInner<TValue>(key, out var value),
                Value = value
            });
        }

        public override TValue Get<TValue>(string key, Func<TValue> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            TValue OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                var valueTimestamp = DateTimeOffset.UtcNow;
                value = getter();
                SetScoped(key, value, valueTimestamp);
                return value;
            }

            if (Policy.DoNotLock)
                return OnCacheMiss();

            return this.Locks.LockAndLoad(key, OnCacheMiss);
        }

        public override async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            async Task<TValue> OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                var valueTimestamp = DateTimeOffset.UtcNow;
                value = await getter().ConfigureAwait(false);
                SetScoped(key, value, valueTimestamp);
                return value;
            }

            if (Policy.DoNotLock)
                return await OnCacheMiss().ConfigureAwait(false);

            return await this.Locks.LockAndLoadAsync(key, OnCacheMiss).ConfigureAwait(false);
        }
    }
}
