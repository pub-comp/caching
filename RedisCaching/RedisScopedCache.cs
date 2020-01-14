using PubComp.Caching.Core;
using System;
using System.Threading.Tasks;
// ReSharper disable NotAccessedField.Local

namespace PubComp.Caching.RedisCaching
{
    public class RedisScopedCache : IScopedCache
    {
        private readonly bool useSlidingExpiration;
        private readonly TimeSpan? expireWithin;
        private readonly DateTime? expireAt;

        private readonly CacheSynchronizer synchronizer;

        public string Name { get; }
        private ScopedCacheContext InnerCache { get; }

        public RedisScopedCache(String name, RedisCachePolicy policy)
        {
            this.Name = name;
            
            var log = NLog.LogManager.GetLogger(typeof(RedisCache).FullName);
            log.Debug("Init Cache {0}", this.Name);

            if (policy == null)
            {
                log.Error("Invalid Policy for Cache {0}", this.Name);
                throw new ArgumentNullException(nameof(policy));
            }

            if (string.IsNullOrEmpty(policy.ConnectionName) && string.IsNullOrEmpty(policy.ConnectionString))
            {
                    throw new ArgumentException(
                    $"{nameof(policy.ConnectionName)} is undefined", $"{nameof(policy)}.{nameof(policy.ConnectionName)}");
            }

            if (policy.SlidingExpiration.HasValue && policy.SlidingExpiration.Value < TimeSpan.MaxValue)
            {
                this.expireWithin = policy.SlidingExpiration.Value;
                this.useSlidingExpiration = true;
                this.expireAt = null;
            }
            else if (policy.ExpirationFromAdd.HasValue && policy.ExpirationFromAdd.Value < TimeSpan.MaxValue)
            {
                this.expireWithin = policy.ExpirationFromAdd.Value;
                this.useSlidingExpiration = false;
                this.expireAt = null;
            }
            else if (policy.AbsoluteExpiration.HasValue && policy.AbsoluteExpiration.Value < DateTimeOffset.MaxValue)
            {
                this.expireWithin = null;
                this.useSlidingExpiration = false;
                this.expireAt = policy.AbsoluteExpiration.Value.LocalDateTime;
            }
            else
            {
                this.expireWithin = null;
                this.useSlidingExpiration = false;
                this.expireAt = null;
            }

            this.useSlidingExpiration = (policy.SlidingExpiration < TimeSpan.MaxValue);

            if (string.IsNullOrEmpty(policy.ConnectionName))
            {
                this.InnerCache = new ScopedCacheContext(
                    policy.ConnectionString, policy.Converter, policy.ClusterType, policy.MonitorPort, policy.MonitorIntervalMilliseconds);
            }
            else
            {
                this.InnerCache = new ScopedCacheContext(policy.ConnectionName, policy.Converter);
            }

            synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, policy.SyncProvider);
        }

        private ScopedCacheItem<TValue> CreateCacheItem<TValue>(string key, TValue value, DateTimeOffset valueTimestamp)
        {
            ScopedCacheItem<TValue> newItem;

            if (!expireAt.HasValue && expireWithin.HasValue)
                newItem = new ScopedCacheItem<TValue>(this.Name, key, value, valueTimestamp, expireWithin.Value);
            else if (expireAt.HasValue)
                newItem = new ScopedCacheItem<TValue>(this.Name, key, value, valueTimestamp, expireAt.Value.Subtract(DateTime.Now));
            else
                newItem = new ScopedCacheItem<TValue>(this.Name, key, value, valueTimestamp);

            return newItem;
        }

        private void ResetExpirationTime<TValue>(ScopedCacheItem<TValue> cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                InnerCache.SetIfNotExists(cacheItem);
                cacheItem.ExpireIn = this.expireWithin.Value;
                InnerCache.SetExpirationTime(cacheItem);
            }
        }

        private async Task ResetExpirationTimeAsync<TValue>(ScopedCacheItem<TValue> cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                await InnerCache.SetIfNotExistsAsync(cacheItem).ConfigureAwait(false);
                cacheItem.ExpireIn = expireWithin.Value;
                await InnerCache.SetExpirationTimeAsync(cacheItem).ConfigureAwait(false);
            }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner(key, out value);
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            var outcome = await TryGetScopedInnerAsync<TValue>(key).ConfigureAwait(false);
            return new TryGetResult<TValue>
            {
                WasFound = outcome.Outcome.MethodTaken.HasFlag(CacheMethodTaken.Get),
                Value = outcome.Value
            };
        }

        private async Task<TryGetScopedResult<TValue>> TryGetScopedInnerAsync<TValue>(String key)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                return new TryGetScopedResult<TValue>
                {
                    Value = default,
                    Outcome = new CacheDirectivesOutcome(CacheMethodTaken.None)
                };
            }

            var cacheItem = await InnerCache.GetItemAsync<TValue>(this.Name, key).ConfigureAwait(false);
            if (cacheItem != null && cacheItem.ValueTimestamp >= directives.MinimumValueTimestamp)
            {
                await ResetExpirationTimeAsync(cacheItem).ConfigureAwait(false);
                return new TryGetScopedResult<TValue>
                {
                    Value = cacheItem.Value,
                    Outcome = new CacheDirectivesOutcome(CacheMethodTaken.Get)
                };
            }

            return new TryGetScopedResult<TValue>
            {
                Value = default,
                Outcome = new CacheDirectivesOutcome(CacheMethodTaken.GetMiss)
            };
        }

        public void Set<TValue>(string key, TValue value)
        {
            var valueTimestamp = ScopedContext<CacheDirectives>.CurrentTimestamp;
            SetScoped(key, value, valueTimestamp);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            var valueTimestamp = ScopedContext<CacheDirectives>.CurrentTimestamp;
            return SetScopedAsync(key, value, valueTimestamp);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            var outcome = TryGetScopedInner(key, out value);
            return outcome.MethodTaken.HasFlag(CacheMethodTaken.Get);
        }

        private CacheDirectivesOutcome TryGetScopedInner<TValue>(String key, out TValue value)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default;
                return new CacheDirectivesOutcome(CacheMethodTaken.None);
            }

            var cacheItem = InnerCache.GetItem<TValue>(this.Name, key);
            if (cacheItem != null && cacheItem.ValueTimestamp >= directives.MinimumValueTimestamp)
            {
                value = cacheItem.Value;
                ResetExpirationTime(cacheItem);
                return new CacheDirectivesOutcome(CacheMethodTaken.Get);
            }

            value = default;
            return new CacheDirectivesOutcome(CacheMethodTaken.GetMiss);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            TValue value;
            if (TryGetInner(key, out value))
                return value;

            var valueTimestamp = DateTimeOffset.UtcNow;
            value = getter();
            SetScoped(key, value, valueTimestamp);
            return value;
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            var result = await TryGetAsync<TValue>(key).ConfigureAwait(false);
            if (result.WasFound)
                return result.Value;

            var valueTimestamp = DateTimeOffset.UtcNow;
            TValue value = await getter().ConfigureAwait(false);
            await SetScopedAsync(key, value, valueTimestamp).ConfigureAwait(false);
            return value;
        }

        public CacheDirectivesOutcome SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                var newItem = CreateCacheItem(key, value, valueTimestamp);
                InnerCache.SetItem(newItem);
                return new CacheDirectivesOutcome(CacheMethodTaken.Set, valueTimestamp);
            }

            return new CacheDirectivesOutcome(CacheMethodTaken.None);
        }

        public async Task<CacheDirectivesOutcome> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = ScopedContext<CacheDirectives>.CurrentContext;
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                var newItem = CreateCacheItem(key, value, valueTimestamp);
                await InnerCache.SetItemAsync(newItem).ConfigureAwait(false);
                return new CacheDirectivesOutcome(CacheMethodTaken.Set, valueTimestamp);
            }

            return new CacheDirectivesOutcome(CacheMethodTaken.None);
        }

        public CacheDirectivesOutcome TryGetScoped<TValue>(String key, out TValue value)
        {
            return TryGetScopedInner(key, out value);
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(String key)
        {
            return TryGetScopedInnerAsync<TValue>(key);
        }

        public void Clear(String key)
        {
            InnerCache.RemoveItem(Name, key);
        }

        public Task ClearAsync(string key)
        {
            return InnerCache.RemoveItemAsync(Name, key);
        }

        public void ClearAll()
        {
            InnerCache.ClearItems(Name);
        }

        public Task ClearAllAsync()
        {
            return InnerCache.ClearItemsAsync(Name);
        }
    }
}
