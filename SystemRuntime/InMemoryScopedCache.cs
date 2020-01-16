using PubComp.Caching.Core;
using System;
using System.Threading.Tasks;
// ReSharper disable RedundantArgumentDefaultValue

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
            SetScoped(key, value, valueTimestamp);
        }

        protected override bool TryGetInner<TValue>(string key, out TValue value)
        {
            var cacheMethodTaken = TryGetScoped(key, out ScopedCacheItem<TValue> scopedCacheItem);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                value = scopedCacheItem.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override TValue Get<TValue>(string key, Func<TValue> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            TValue OnCacheMiss()
            {
                var valueTimestamp = DateTimeOffset.UtcNow;
                value = getter();
                SetScoped(key, value, valueTimestamp);
                return value;
            }

            TValue OnCacheMissWithLock()
            {
                if (TryGetInner(key, out value)) return value;
                return OnCacheMiss();
            }

            if (Policy.DoNotLock)
                return OnCacheMiss();

            return this.Locks.LockAndLoad(key, OnCacheMissWithLock);
        }

        public ScopedCacheItem<TValue> GetScoped<TValue>(string key, Func<ScopedCacheItem<TValue>> getter)
        {
            var cacheMethodTaken = TryGetScoped<TValue>(key, out var scopedCacheItem);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                return new TryGetScopedResult<TValue>
                {
                    ScopedCacheItem = scopedCacheItem,
                    MethodTaken = CacheMethodTaken.Get
                };
            }

            TryGetScopedResult<TValue> OnCacheMiss()
            {
                var getterScopedResult = getter(); 
                var cacheMethodTakenOnMiss = SetScoped(key, getterScopedResult);
                return new TryGetScopedResult<TValue>
                {
                    ScopedCacheItem = getterScopedResult,
                    MethodTaken = cacheMethodTakenOnMiss | CacheMethodTaken.GetMiss
                };
            }

            TryGetScopedResult<TValue> OnCacheMissWithLock()
            {
                var cacheMethodTakenOnMiss = TryGetScoped<TValue>(key, out var scopedCacheValueOnMiss);
                if (cacheMethodTakenOnMiss.HasFlag(CacheMethodTaken.Get))
                {
                    return new TryGetScopedResult<TValue>
                    {
                        ScopedCacheItem = scopedCacheValueOnMiss,
                        MethodTaken = CacheMethodTaken.Get
                    };
                }

                return OnCacheMiss();
            }

            if (Policy.DoNotLock)
                return OnCacheMiss();

            return this.Locks.LockAndLoad(key, OnCacheMissWithLock);
        }

        public override async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            async Task<ScopedCacheItem<TValue>> GetterWrapper()
            {
                var valueTimestamp = DateTimeOffset.UtcNow;
                var value = await getter().ConfigureAwait(false);
                return new ScopedCacheItem<TValue>
                {
                    Value = value,
                    ValueTimestamp = valueTimestamp
                };
            }

            var scopedCacheItem = await GetScopedAsync(key, GetterWrapper).ConfigureAwait(false);
            return scopedCacheItem.Value;
        }

        public async Task<ScopedCacheItem<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedCacheItem<TValue>>> getter)
        {
            var cacheMethodTaken = TryGetScoped(key, out ScopedCacheItem<TValue> scopedCacheItem);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
                return scopedCacheItem;

            async Task<ScopedCacheItem<TValue>> OnCacheMiss()
            {
                var getterScopedCacheItem = await getter().ConfigureAwait(false);
                SetScoped(key, getterScopedCacheItem);
                return getterScopedCacheItem;
            }

            async Task<ScopedCacheItem<TValue>> OnCacheMissWithLock()
            {
                var cacheMethodTakenOnMiss = TryGetScoped(key, out ScopedCacheItem<TValue> scopedCacheValueOnMiss);
                if (cacheMethodTakenOnMiss.HasFlag(CacheMethodTaken.Get)) 
                    return scopedCacheValueOnMiss;
                return await OnCacheMiss().ConfigureAwait(false);
            }

            if (Policy.DoNotLock)
                return await OnCacheMiss().ConfigureAwait(false);

            return await this.Locks.LockAndLoadAsync(key, OnCacheMissWithLock).ConfigureAwait(false);
        }

        public CacheMethodTaken SetScoped<TValue>(String key, ScopedCacheItem<TValue> scopedCacheItem)
            => SetScoped(key, scopedCacheItem.Value, scopedCacheItem.ValueTimestamp);

        public CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                InnerCache.Set(key, new ScopedCacheItem<TValue>(value, valueTimestamp), GetRuntimePolicy(), regionName: null);
                return CacheMethodTaken.Set;
            }

            return CacheMethodTaken.None;
        }

        public Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var cacheMethodTaken = SetScoped(key, value, valueTimestamp);
            return Task.FromResult(cacheMethodTaken);
        }

        public CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedCacheItem<TValue> value)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default;
                return CacheMethodTaken.None;
            }

            if (InnerCache.Get(key, regionName: null) is ScopedCacheItem<TValue> scopedCacheItem
                && scopedCacheItem.ValueTimestamp >= directives.MinimumValueTimestamp)
            {
                value = scopedCacheItem;
                return CacheMethodTaken.Get;
            }

            value = default;
            return CacheMethodTaken.GetMiss;
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key)
        {
            var cacheMethodTaken = TryGetScoped<TValue>(key, out var scopedCacheItem);
            return Task.FromResult(new TryGetScopedResult<TValue>
            {
                ScopedCacheItem = scopedCacheItem,
                MethodTaken = cacheMethodTaken
            });
        }
    }
}
