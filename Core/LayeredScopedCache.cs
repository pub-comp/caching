using PubComp.Caching.Core.Notifications;
using System;
using System.Threading.Tasks;
// ReSharper disable NotAccessedField.Local
// ReSharper disable LocalVariableHidesMember
// ReSharper disable UseStringInterpolation

namespace PubComp.Caching.Core
{
    /// <summary>
    /// A layered cache e.g. level1 = in-memory cache that falls back to level2 = distributed cache
    /// </summary>
    public class LayeredScopedCache : IScopedCache
    {
        private readonly String name;
        private readonly IScopedCache level1;
        private readonly IScopedCache level2;
        private readonly LayeredCachePolicy policy;
        private readonly ICacheNotifier level1Notifier;

        private readonly CacheSynchronizer synchronizer;

        public LayeredScopedCache(String name, LayeredCachePolicy policy)
            : this(name, policy?.Level1CacheName, policy?.Level2CacheName)
        {
            this.policy = policy;

            if (policy?.InvalidateLevel1OnLevel2Update ?? false)
            {
                level1Notifier = CacheManager.GetNotifier(policy.Level1CacheName);
                if (level1Notifier == null)
                    throw new ApplicationException("SyncProvider is not registered for automatic invalidation policy: level1CacheName=" + policy.Level1CacheName);
            }
        }

        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1CacheName">Name of first cache to check (e.g. in-memory cache), should be registered in CacheManager</param>
        /// <param name="level2CacheName">Name of fallback cache (e.g. distributed cache), should be registered in CacheManager</param>
        public LayeredScopedCache(String name, String level1CacheName, String level2CacheName)
        {
            this.name = name;

            var level1 = CacheManager.GetCache(level1CacheName);
            if (level1 == null)
                throw new ApplicationException("Cache is not registered: level1CacheName=" + level1CacheName);
            if (!(level1 is IScopedCache))
                throw new ApplicationException($"Cache type does not implement {nameof(IScopedCache)}: level1CacheName={level1CacheName}");

            var level2 = CacheManager.GetCache(level2CacheName);
            if (level2 == null)
                throw new ApplicationException("Cache is not registered: level2CacheName=" + level2CacheName);
            if (!(level2 is IScopedCache))
                throw new ApplicationException($"Cache type does not implement {nameof(IScopedCache)}: level2CacheName={level2CacheName}");

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("level2 must not be the same as level1, received {0}={1}, {2}={3}, which map to {4} and {5}",
                        "level1CacheName", level1CacheName,
                        "level2CacheName", level2CacheName,
                        level1.Name,
                        level2.Name));
            }

            this.level1 = (IScopedCache)level1;
            this.level2 = (IScopedCache)level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1CacheName, Level2CacheName = level1CacheName };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy?.SyncProvider);
        }

        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1">First cache to check (e.g. in-memory cache)</param>
        /// <param name="level2">Fallback cache (e.g. distributed cache)</param>
        public LayeredScopedCache(String name, ICache level1, ICache level2)
        {
            this.name = name;

            if (level1 == null)
                throw new ApplicationException("innerCache1 must not be null");
            if (!(level1 is IScopedCache))
                throw new ApplicationException($"innerCache1 type does not implement {nameof(IScopedCache)}");

            if (level2 == null)
                throw new ApplicationException("innerCache2 must not be null");
            if (!(level2 is IScopedCache))
                throw new ApplicationException($"innerCache2 type does not implement {nameof(IScopedCache)}");

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("level2 must not be the same as level1, received {0}={1} and {2}={3}",
                        "level1", level1.Name, "level2", level2.Name));
            }

            this.level1 = (IScopedCache)level1;
            this.level2 = (IScopedCache)level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1.Name, Level2CacheName = level2.Name };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy?.SyncProvider);
        }

        public string Name { get { return this.name; } }

        protected ICache Level1 { get { return this.level1; } }

        protected ICache Level2 { get { return this.level2; } }

        protected LayeredCachePolicy Policy { get { return this.policy; } }

        public bool TryGet<TValue>(String key, out TValue value)
        {
            var directives = CacheDirectives.CurrentScope;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default;
                return false;
            }

            if (this.level1.TryGet(key, out value))
                return true;

            var level2Result = this.level2.TryGetScoped(key, out ScopedValue<TValue> scopedCacheItem);
            if (level2Result.HasFlag(CacheMethodTaken.Get))
            {
                this.level1.SetScoped(key, scopedCacheItem.Value, scopedCacheItem.ValueTimestamp);
                return true;
            }

            value = default;
            return false;
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(String key)
        {
            var directives = CacheDirectives.CurrentScope;
            if (!directives.Method.HasFlag(CacheMethod.Get))
                return new TryGetResult<TValue> { WasFound = false };

            var level1Result = await this.level1.TryGetAsync<TValue>(key).ConfigureAwait(false);
            if (level1Result.WasFound)
                return level1Result;

            var level2Result = await this.level2.TryGetScopedAsync<TValue>(key).ConfigureAwait(false);
            if (level2Result.MethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                await this.level1
                    .SetScopedAsync(key, level2Result.ScopedValue.Value, level2Result.ScopedValue.ValueTimestamp)
                    .ConfigureAwait(false);
                return new TryGetResult<TValue>
                {
                    WasFound = true,
                    Value = level2Result.ScopedValue.Value
                };
            }

            return new TryGetResult<TValue> { WasFound = false };
        }

        public void Set<TValue>(String key, TValue value)
        {
            var valueTimestamp = CacheDirectives.CurrentScopeTimestamp;
            SetScoped(key, value, valueTimestamp);
        }

        public async Task SetAsync<TValue>(String key, TValue value)
        {
            var valueTimestamp = CacheDirectives.CurrentScopeTimestamp;
            await SetScopedAsync(key, value, valueTimestamp).ConfigureAwait(false);
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            ScopedValue<TValue> GetterWrapper()
            {
                var valueTimestamp = DateTimeOffset.UtcNow;
                return new ScopedValue<TValue>
                {
                    ValueTimestamp = valueTimestamp,
                    Value = getter()
                };
            }

            return GetScoped(key, GetterWrapper).ScopedValue.Value;
        }

        public GetScopedResult<TValue> GetScoped<TValue>(String key, Func<ScopedValue<TValue>> getter)
        {
            GetScopedResult<TValue> level2Result = null;
            var level1Result = this.level1.GetScoped(key, () =>
            {
                level2Result = this.level2.GetScoped<TValue>(key, getter);
                return level2Result.ScopedValue;
            });

            if (level2Result != null
                && level2Result.MethodTaken.HasFlag(CacheMethodTaken.Set)
                && this.policy.InvalidateLevel1OnLevel2Update)
                this.level1Notifier.Publish(policy.Level1CacheName, key, CacheItemActionTypes.Updated);

            return level1Result;
        }

        public async Task<TValue> GetAsync<TValue>(String key, Func<Task<TValue>> getter)
        {
            async Task<ScopedValue<TValue>> scopedGetter()
            {
                var valueTimestamp = DateTimeOffset.UtcNow;
                var value = await getter().ConfigureAwait(false);
                return new ScopedValue<TValue>
                {
                    ValueTimestamp = valueTimestamp,
                    Value = value
                };
            }

            var getScopedResult = await GetScopedAsync(key, scopedGetter).ConfigureAwait(false);
            return getScopedResult.ScopedValue.Value;
        }

        public async Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(String key, Func<Task<ScopedValue<TValue>>> getter)
        {
            GetScopedResult<TValue> level2Result = null;

            var level1Result = await this.level1.GetScopedAsync(key, async () =>
            {
                level2Result = await this.level2.GetScopedAsync(key, getter).ConfigureAwait(false);
                return level2Result.ScopedValue;
            }).ConfigureAwait(false);

            if (level2Result != null && level2Result.MethodTaken.HasFlag(CacheMethodTaken.Set) 
                && this.policy.InvalidateLevel1OnLevel2Update)
                await this.level1Notifier
                    .PublishAsync(policy.Level1CacheName, key, CacheItemActionTypes.Updated)
                    .ConfigureAwait(false);

            return level1Result;
        }

        public CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            this.level1.SetScoped(key, value, valueTimestamp);
            var level2Result = this.level2.SetScoped(key, value, valueTimestamp);

            if (level2Result.HasFlag(CacheMethodTaken.Set) && this.policy.InvalidateLevel1OnLevel2Update)
                level1Notifier.Publish(policy.Level1CacheName, key, CacheItemActionTypes.Updated);

            return level2Result;
        }

        public async Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            await this.level1.SetScopedAsync(key, value, valueTimestamp).ConfigureAwait(false);
            var level2Result = await this.level2.SetScopedAsync(key, value, valueTimestamp).ConfigureAwait(false);

            if (this.policy.InvalidateLevel1OnLevel2Update && level2Result.HasFlag(CacheMethodTaken.Set))
                await level1Notifier.PublishAsync(policy.Level1CacheName, key, CacheItemActionTypes.Updated).ConfigureAwait(false);

            return level2Result;
        }

        public CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedValue<TValue> scopedValue)
        {
            var level1Result = this.level1.TryGetScoped(key, out scopedValue);
            if (level1Result.HasFlag(CacheMethodTaken.Get))
                return level1Result;

            var level2Result = this.level2.TryGetScoped(key, out scopedValue);
            if (level2Result.HasFlag(CacheMethodTaken.Get))
                this.level1.SetScoped(key, scopedValue.Value, scopedValue.ValueTimestamp);

            return level2Result;
        }

        public async Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key)
        {
            var level1Result = await this.level1.TryGetScopedAsync<TValue>(key).ConfigureAwait(false);
            if (level1Result.MethodTaken.HasFlag(CacheMethodTaken.Get))
                return level1Result;

            var level2Result = await this.level2.TryGetScopedAsync<TValue>(key).ConfigureAwait(false);
            if (level2Result.MethodTaken.HasFlag(CacheMethodTaken.Get))
                await this.level1
                    .SetScopedAsync(key, level2Result.ScopedValue.Value, level2Result.ScopedValue.ValueTimestamp)
                    .ConfigureAwait(false);

            return level2Result;
        }

        public void Clear(String key)
        {
            this.level2.Clear(key);
            this.level1.Clear(key);
        }

        public async Task ClearAsync(String key)
        {
            await this.level2.ClearAsync(key).ConfigureAwait(false);
            await this.level1.ClearAsync(key).ConfigureAwait(false);
        }

        public void ClearAll()
        {
            this.level2.ClearAll();
            this.level1.ClearAll();
        }

        public async Task ClearAllAsync()
        {
            await this.level2.ClearAllAsync().ConfigureAwait(false);
            await this.level1.ClearAllAsync().ConfigureAwait(false);
        }
    }
}
