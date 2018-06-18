using System;
using System.Threading.Tasks;
using PubComp.Caching.Core;
using PubComp.NoSql.MongoDbDriver;

namespace PubComp.Caching.MongoDbCaching
{
    public class MongoDbCache : ICache
    {
        private readonly String connectionString;
        private readonly String cacheDbName;
        private readonly String cacheCollectionName;
        private readonly bool useSlidingExpiration;
        private readonly TimeSpan? expireWithin;
        private readonly DateTime? expireAt;
        private readonly CacheSynchronizer synchronizer;

        public MongoDbCache(String cacheCollectionName, MongoDbCachePolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

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

            this.cacheDbName = !string.IsNullOrEmpty(policy.DatabaseName)
                ? policy.DatabaseName : "CacheDb";
            
            this.cacheCollectionName = cacheCollectionName;

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

            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, policy.SyncProvider);
        }

        public string Name { get { return this.cacheCollectionName; } }

        private CacheContext GetContext()
        {
            return new CacheContext(this.connectionString, this.cacheDbName);
        }

        private MongoDbContext.EntitySet<String, CacheItem> GetEntitySet(CacheContext context)
        {
            if (expireWithin.HasValue || expireAt.HasValue)
            {
                return context.GetEntitySet(
                    this.cacheDbName, this.cacheCollectionName, this.expireWithin ?? TimeSpan.FromSeconds(0));
            }

            return context.GetEntitySet(this.cacheDbName, this.cacheCollectionName, null);
        }

        private TValue GetOrAdd<TValue>(
            CacheContext context, string key, TValue newValue, bool doForceOverride = false)
        {
            var set = GetEntitySet(context);
            var newItem = CreateCacheItem(key, newValue);

            try
            {
                set.Add(newItem);
                return newValue;
            }
            catch (MongoDB.Driver.MongoDuplicateKeyException)
            {
                var prevValue = GetCacheItem(set, key);
                if (!doForceOverride && prevValue != null && prevValue.Value is TValue)
                    return (TValue)prevValue.Value;
                set.AddOrUpdate(newItem);
                return newValue;
            }
            catch (NoSql.Core.DalFailure)
            {
                var prevValue = GetCacheItem(set, key);
                if (prevValue != null && prevValue.Value is TValue)
                    return (TValue)prevValue.Value;
                set.AddOrUpdate(newItem);
                return newValue;
            }
        }

        private CacheItem CreateCacheItem<TValue>(string key, TValue value)
        {
            CacheItem newItem;

            if (!expireAt.HasValue && expireWithin.HasValue)
                newItem = new CacheItem(key, value, DateTime.Now);
            else
                newItem = new CacheItem(key, value, expireAt);

            return newItem;
        }

        private CacheItem GetCacheItem(MongoDbContext.EntitySet<String, CacheItem> set, string key)
        {
            var cacheItem = set.Get(key);
            return cacheItem;
        }

        private void UpdateExpirationTime(MongoDbContext.EntitySet<String, CacheItem> set, CacheItem cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                cacheItem.ExpireAt = DateTime.Now;
                set.UpdateField(cacheItem, "ExpireAt");
            }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner(key, out value);
        }

        public Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            // TODO: This should be made async -- requires updating MongoDbDriver
            var result = TryGetInner(key, out TValue value);
            return Task.FromResult(new TryGetResult<TValue>{WasFound = result, Value = value});
        }

        public void Set<TValue>(string key, TValue value)
        {
            Add(key, value);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            // TODO: This should be made async -- requires updating MongoDbDriver
            Set(key, value);
            return Task.FromResult<object>(null);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                var cacheItem = GetCacheItem(set, key);

                if (cacheItem != null)
                {
                    // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                    value = cacheItem.Value is TValue ? (TValue)cacheItem.Value : default(TValue);
                    UpdateExpirationTime(set, cacheItem);
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            using (var context = GetContext())
            {
                GetOrAdd(context, key, value, true);
            }
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            TValue value;
            if (TryGetInner(key, out value))
                return value;

            using (var context = GetContext())
            {
                value = getter();
                return GetOrAdd(context, key, value);
            }
        }

        public Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            // TODO: This should be made async -- requires updating MongoDbDriver
            return Task.FromResult(Get(key, () => getter().Result));
        }

        public void Clear(string key)
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                set.Delete(key);
            }
        }

        public Task ClearAsync(string key)
        {
            // TODO: This should be made async -- requires updating MongoDbDriver
            Clear(key);
            return Task.FromResult<object>(null);
        }

        public void ClearAll()
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                set.Delete(i => true);
            }
        }

        public Task ClearAllAsync()
        {
            // TODO: This should be made async -- requires updating MongoDbDriver
            ClearAll();
            return Task.FromResult<object>(null);
        }
    }
}
