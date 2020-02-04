using PubComp.Caching.Core;
using System;
using System.Threading.Tasks;
// ReSharper disable RedundantArgumentDefaultValue

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryScopedCache : ObjectCache, IScopedCache
    {
        public bool IsActive { get; } = true;

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
            var valueTimestamp = CacheDirectives.CurrentScopeTimestamp;
            SetScoped(key, value, valueTimestamp);
        }

        protected override bool TryGetInner<TValue>(string key, out TValue value)
        {
            var cacheMethodTaken = TryGetScopedInner(key, out ScopedValue<TValue> scopedValue);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                value = scopedValue.Value;
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

        public GetScopedResult<TValue> GetScoped<TValue>(string key, Func<ScopedValue<TValue>> getter)
        {
            var cacheMethodTaken = TryGetScoped<TValue>(key, out var scopedValue);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                return new GetScopedResult<TValue>
                {
                    ScopedValue = scopedValue,
                    MethodTaken = CacheMethodTaken.Get
                };
            }

            GetScopedResult<TValue> OnCacheMiss()
            {
                var getterScopedResult = getter(); 
                var cacheMethodTakenOnMiss = SetScoped(key, getterScopedResult);
                return new GetScopedResult<TValue>
                {
                    ScopedValue = getterScopedResult,
                    MethodTaken = cacheMethodTakenOnMiss | cacheMethodTaken
                };
            }

            GetScopedResult<TValue> OnCacheMissWithLock()
            {
                var cacheMethodTakenOnMiss = TryGetScoped<TValue>(key, out var scopedCacheValueOnMiss);
                if (cacheMethodTakenOnMiss.HasFlag(CacheMethodTaken.Get))
                {
                    return new GetScopedResult<TValue>
                    {
                        ScopedValue = scopedCacheValueOnMiss,
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
            async Task<ScopedValue<TValue>> GetterWrapper()
            {
                var valueTimestamp = DateTimeOffset.UtcNow;
                var value = await getter().ConfigureAwait(false);
                return new ScopedValue<TValue>
                {
                    Value = value,
                    ValueTimestamp = valueTimestamp
                };
            }

            var scopedValue = await GetScopedAsync(key, GetterWrapper).ConfigureAwait(false);
            return scopedValue.ScopedValue.Value;
        }

        public async Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedValue<TValue>>> getter)
        {
            var cacheMethodTaken = TryGetScoped(key, out ScopedValue<TValue> scopedValue);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
                return new GetScopedResult<TValue>
                {
                    ScopedValue = scopedValue,
                    MethodTaken = cacheMethodTaken
                };

            async Task<GetScopedResult<TValue>> OnCacheMiss()
            {
                var scopedValueOnMiss = await getter().ConfigureAwait(false);
                cacheMethodTaken |= SetScoped(key, scopedValueOnMiss.Value, scopedValueOnMiss.ValueTimestamp);
                return new GetScopedResult<TValue>
                {
                    ScopedValue = scopedValueOnMiss,
                    MethodTaken = cacheMethodTaken
                };
            }

            async Task<GetScopedResult<TValue>> OnCacheMissWithLock()
            {
                var cacheMethodTakenOnMiss = TryGetScoped(key, out ScopedValue<TValue> scopedValueOnMiss);
                if (cacheMethodTakenOnMiss.HasFlag(CacheMethodTaken.Get)) 
                    return new GetScopedResult<TValue>
                    {
                        ScopedValue = scopedValueOnMiss,
                        MethodTaken = cacheMethodTakenOnMiss
                    };
                return await OnCacheMiss().ConfigureAwait(false);
            }

            if (Policy.DoNotLock)
                return await OnCacheMiss().ConfigureAwait(false);

            return await this.Locks.LockAndLoadAsync(key, OnCacheMissWithLock).ConfigureAwait(false);
        }

        public CacheMethodTaken SetScoped<TValue>(String key, ScopedValue<TValue> scopedValue)
            => SetScoped(key, scopedValue.Value, scopedValue.ValueTimestamp);

        public CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = CacheDirectives.CurrentScope;
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                InnerCache.Set(key, new ScopedValue<TValue>(value, valueTimestamp), GetRuntimePolicy(), regionName: null);
                return CacheMethodTaken.Set;
            }

            return CacheMethodTaken.None;
        }

        public Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var cacheMethodTaken = SetScoped(key, value, valueTimestamp);
            return Task.FromResult(cacheMethodTaken);
        }

        protected virtual CacheMethodTaken TryGetScopedInner<TValue>(String key, out ScopedValue<TValue> value)
        {
            var directives = CacheDirectives.CurrentScope;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default;
                return CacheMethodTaken.None;
            }

            if (InnerCache.Get(key, regionName: null) is ScopedValue<TValue> scopedValue
                && directives.IsInScope(scopedValue))
            {
                value = scopedValue;
                return CacheMethodTaken.Get;
            }

            value = default;
            return CacheMethodTaken.GetMiss;
        }

        public CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedValue<TValue> value)
        {
            return TryGetScopedInner(key, out value);
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key)
        {
            var cacheMethodTaken = TryGetScopedInner<TValue>(key, out var scopedValue);
            return Task.FromResult(new TryGetScopedResult<TValue>
            {
                ScopedValue = scopedValue,
                MethodTaken = cacheMethodTaken
            });
        }
    }
}
