﻿using PubComp.Caching.Core;
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
        private readonly RedisCachePolicy Policy;
        private readonly CacheSynchronizer synchronizer;
        
        private ScopedCacheContext InnerCache { get; }

        public string Name { get; }
        public bool IsActive => InnerCache.IsActive;

        public RedisScopedCache(String name, RedisCachePolicy policy)
        {
            this.Name = name;
            var log = NLog.LogManager.GetLogger(typeof(RedisScopedCache).FullName);
            this.Policy = policy;

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

        private CacheDirectives GetCacheDirectives()
        {
            var directives = CacheDirectives.CurrentScope;
            if (!this.IsActive) directives.Method &= ~CacheMethod.GetOrSet;
            return directives;
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
            var result = await TryGetScopedInnerAsync<TValue>(key).ConfigureAwait(false);
            return result.MethodTaken.HasFlag(CacheMethodTaken.Get)
                ? new TryGetResult<TValue> { WasFound = true, Value = result.ScopedValue.Value }
                : new TryGetResult<TValue> { WasFound = false, Value = default };
        }

        private async Task<TryGetScopedResult<TValue>> TryGetScopedInnerAsync<TValue>(String key)
        {
            var directives = GetCacheDirectives();
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                return new TryGetScopedResult<TValue>
                {
                    ScopedValue = default,
                    MethodTaken = CacheMethodTaken.None
                };
            }

            var scopedCacheItem = await InnerCache.GetItemAsync<TValue>(this.Name, key).ConfigureAwait(false);
            if (scopedCacheItem != null && directives.IsInScope(scopedCacheItem))
            {
                await ResetExpirationTimeAsync(scopedCacheItem).ConfigureAwait(false);
                return new TryGetScopedResult<TValue>
                {
                    ScopedValue = scopedCacheItem,
                    MethodTaken = CacheMethodTaken.Get
                };
            }

            return new TryGetScopedResult<TValue>
            {
                ScopedValue = default,
                MethodTaken = CacheMethodTaken.GetMiss
            };
        }

        public void Set<TValue>(string key, TValue value)
        {
            var valueTimestamp = CacheDirectives.CurrentScopeTimestamp;
            SetScoped(key, value, valueTimestamp);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            var valueTimestamp = CacheDirectives.CurrentScopeTimestamp;
            return SetScopedAsync(key, value, valueTimestamp);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            var cacheMethodTaken = TryGetScopedInner<TValue>(key, out var scopedValue);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
            {
                value = scopedValue.Value;
                return true;
            }

            value = default;
            return false;
        }

        private CacheMethodTaken TryGetScopedInner<TValue>(String key, out ScopedValue<TValue> value)
        {
            var directives = GetCacheDirectives();
            if (!directives.Method.HasFlag(CacheMethod.Get))
            {
                value = default;
                return CacheMethodTaken.None;
            }

            var scopedCacheItem = InnerCache.GetItem<TValue>(this.Name, key);
            if (scopedCacheItem != null && directives.IsInScope(scopedCacheItem))
            {
                value = scopedCacheItem;
                ResetExpirationTime(scopedCacheItem);
                return CacheMethodTaken.Get;
            }

            value = default;
            return CacheMethodTaken.GetMiss;
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            if (TryGetInner(key, out TValue value))
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

        public GetScopedResult<TValue> GetScoped<TValue>(string key, Func<ScopedValue<TValue>> getter)
        {
            var cacheMethodTaken = TryGetScoped<TValue>(key, out var scopedValue);
            if (cacheMethodTaken.HasFlag(CacheMethodTaken.Get))
                return new GetScopedResult<TValue>
                {
                    ScopedValue = scopedValue,
                    MethodTaken = cacheMethodTaken
                };

            scopedValue = getter();
            cacheMethodTaken |= SetScoped(key, scopedValue.Value, scopedValue.ValueTimestamp);
            return new GetScopedResult<TValue>
            {
                ScopedValue = scopedValue,
                MethodTaken = cacheMethodTaken
            };
        }

        public async Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedValue<TValue>>> getter)
        {
            var getResult = await TryGetScopedAsync<TValue>(key).ConfigureAwait(false);
            if (getResult.MethodTaken.HasFlag(CacheMethodTaken.Get))
                return getResult;

            var scopedValue = await getter().ConfigureAwait(false);
            var cacheMethodTaken = await SetScopedAsync(key, scopedValue.Value, scopedValue.ValueTimestamp).ConfigureAwait(false);
            return new GetScopedResult<TValue>
            {
                ScopedValue = scopedValue,
                MethodTaken = cacheMethodTaken | getResult.MethodTaken
            };
        }

        public CacheMethodTaken SetScoped<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = GetCacheDirectives();
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                var newItem = CreateCacheItem(key, value, valueTimestamp);
                InnerCache.SetItem(newItem);
                return CacheMethodTaken.Set;
            }

            return CacheMethodTaken.None;
        }

        public async Task<CacheMethodTaken> SetScopedAsync<TValue>(String key, TValue value, DateTimeOffset valueTimestamp)
        {
            var directives = GetCacheDirectives();
            if (directives.Method.HasFlag(CacheMethod.Set))
            {
                var newItem = CreateCacheItem(key, value, valueTimestamp);
                await InnerCache.SetItemAsync(newItem).ConfigureAwait(false);
                return CacheMethodTaken.Set;
            }

            return CacheMethodTaken.None;
        }

        public CacheMethodTaken TryGetScoped<TValue>(String key, out ScopedValue<TValue> value)
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

        public object GetDetails() => new
        {
            this.Policy.SyncProvider,
            this.Policy.ConnectionName,
            this.Policy.AbsoluteExpiration,
            this.Policy.SlidingExpiration,
            this.Policy.ExpirationFromAdd,

            UseSlidingExpiration = this.useSlidingExpiration,
            ExpireWithin = this.expireWithin,
            ExpireAt = expireAt
        };
    }
}
