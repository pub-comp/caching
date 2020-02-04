using PubComp.Caching.Core;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCache : ICache, ICacheGetPolicy
    {
        private readonly string name;
        private readonly string connectionString;
        private readonly string notiferName;
        private readonly bool useSlidingExpiration;
        private readonly TimeSpan? expireWithin;
        private readonly DateTime? expireAt;
        private readonly string converterType;
        private readonly string clusterType;
        private readonly int monitorPort;
        private readonly int monitorIntervalMilliseconds;
        private readonly CacheContext innerCache;
        private readonly CacheSynchronizer synchronizer;
        private readonly NLog.ILogger log;

        private readonly RedisCachePolicy Policy;

        public string Name { get { return this.name; } }

        private CacheContext InnerCache
        {
            get { return innerCache; }
        }

        public RedisCache(String name, RedisCachePolicy policy)
        {
            this.name = name;
            this.log = NLog.LogManager.GetLogger(typeof(RedisCache).FullName);
            this.Policy = policy;

            log.Debug("Init Cache {0}", this.name);

            if (policy == null)
            {
                log.Error("Invalid Policy for Cache {0}", this.name);
                throw new ArgumentNullException(nameof(policy));
            }

            if (!string.IsNullOrEmpty(policy.ConnectionName))
            {
                this.connectionString = CacheManager.GetConnectionString(policy.ConnectionName)?.ConnectionString;

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException(
                        $"{nameof(ICacheConnectionString.ConnectionString)} not found for {nameof(policy.ConnectionName)} {policy.ConnectionName}", $"{nameof(policy)}.{nameof(policy.ConnectionName)}");
                }
            }
            else if (!string.IsNullOrEmpty(policy.ConnectionString))
            {
                this.connectionString = policy.ConnectionString;
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(policy.ConnectionString)} is undefined", $"{nameof(policy)}.{nameof(policy.ConnectionString)}");
            }

            this.monitorPort = policy.MonitorPort;
            this.monitorIntervalMilliseconds = policy.MonitorIntervalMilliseconds;
            this.converterType = policy.Converter;
            this.clusterType = policy.ClusterType;

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
                this.innerCache = new CacheContext(
                    this.connectionString, this.converterType, this.clusterType, this.monitorPort, this.monitorIntervalMilliseconds);
            }
            else
            {
                this.innerCache = new CacheContext(policy.ConnectionName, this.converterType);
            }

            this.notiferName = policy.SyncProvider;

            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.notiferName);
        }

        private TValue GetOrAdd<TValue>(
            CacheContext context, string key, TValue newValue, bool doForceOverride = false)
        {
            var newItem = CreateCacheItem(key, newValue);

            if (context.SetIfNotExists(newItem))
            {
                return newValue;
            }

            var prevValue = GetCacheItem<TValue>(context, key);
            if (!doForceOverride && prevValue != null && prevValue.Value is TValue)
                return prevValue.Value;

            context.SetItem(newItem);

            return newValue;
        }

        private async Task<TValue> GetOrAddAsync<TValue>(
            CacheContext context, string key, TValue newValue, bool doForceOverride = false)
        {
            var newItem = CreateCacheItem(key, newValue);

            if (await context.SetIfNotExistsAsync(newItem).ConfigureAwait(false))
            {
                return newValue;
            }

            var prevValue = await GetCacheItemAsync<TValue>(context, key).ConfigureAwait(false);
            if (!doForceOverride && prevValue != null && prevValue.Value is TValue)
                return prevValue.Value;

            await context.SetItemAsync(newItem).ConfigureAwait(false);

            return newValue;
        }

        private CacheItem<TValue> CreateCacheItem<TValue>(string key, TValue value)
        {
            CacheItem<TValue> newItem;

            if (!expireAt.HasValue && expireWithin.HasValue)
                newItem = new CacheItem<TValue>(this.Name, key, value, expireWithin.Value);
            else if (expireAt.HasValue)
                newItem = new CacheItem<TValue>(this.Name, key, value, expireAt.Value.Subtract(DateTime.Now));
            else
                newItem = new CacheItem<TValue>(this.Name, key, value);

            return newItem;
        }

        private CacheItem<TValue> GetCacheItem<TValue>(CacheContext context, string key)
        {
            var cacheItem = context.GetItem<TValue>(this.Name, key);
            return cacheItem;
        }

        private async Task<CacheItem<TValue>> GetCacheItemAsync<TValue>(CacheContext context, string key)
        {
            var cacheItem = await context.GetItemAsync<TValue>(Name, key).ConfigureAwait(false);
            return cacheItem;
        }

        private void ResetExpirationTime<TValue>(CacheContext context, CacheItem<TValue> cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                context.SetIfNotExists(cacheItem);
                cacheItem.ExpireIn = this.expireWithin.Value;
                context.SetExpirationTime(cacheItem);
            }
        }

        private async Task ResetExpirationTimeAsync<TValue>(CacheContext context, CacheItem<TValue> cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                await context.SetIfNotExistsAsync(cacheItem).ConfigureAwait(false);
                cacheItem.ExpireIn = expireWithin.Value;
                await context.SetExpirationTimeAsync(cacheItem).ConfigureAwait(false);
            }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner(key, out value);
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            var cacheItem = await GetCacheItemAsync<TValue>(InnerCache, key).ConfigureAwait(false);

            if (cacheItem != null)
            {
                await ResetExpirationTimeAsync(InnerCache, cacheItem).ConfigureAwait(false);
                return new TryGetResult<TValue>
                {
                    Value = cacheItem.Value,
                    WasFound = true
                };
            }

            return new TryGetResult<TValue>
            {
                Value = default,
                WasFound = false
            };
        }

        public void Set<TValue>(string key, TValue value)
        {
            Add(key, value);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            return AddAsync(key, value);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            var cacheItem = InnerCache.GetItem<TValue>(this.Name, key);

            if (cacheItem != null)
            {
                value = cacheItem.Value;
                ResetExpirationTime(InnerCache, cacheItem);
                return true;
            }

            value = default;
            return false;
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            GetOrAdd(InnerCache, key, value, true);
        }

        protected virtual Task AddAsync<TValue>(String key, TValue value)
        {
            return GetOrAddAsync(InnerCache, key, value, true);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            TValue value;
            if (TryGetInner(key, out value))
                return value;

            value = getter();
            return GetOrAdd(InnerCache, key, value);
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            var result = await TryGetAsync<TValue>(key).ConfigureAwait(false);
            if (result.WasFound)
                return result.Value;

            result.Value = await getter().ConfigureAwait(false);
            return await GetOrAddAsync(InnerCache, key, result.Value).ConfigureAwait(false);
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

        public object GetPolicy()
        {
            return new
            {
                this.Policy.ConnectionName,
                this.Policy.AbsoluteExpiration,
                this.Policy.SlidingExpiration,
                this.Policy.ExpirationFromAdd,
                this.Policy.SyncProvider,

                UseSlidingExpiration = this.useSlidingExpiration,
                ExpireWithin = this.expireWithin,
                ExpireAt = expireAt
            };
        }
    }
}
