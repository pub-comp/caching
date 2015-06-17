using System;
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

        public MongoDbCache(String cacheCollectionName, MongoDbCachePolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException("policy");

            this.connectionString = policy.ConnectionString;
            
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
            else
            {
                return context.GetEntitySet(this.cacheDbName, this.cacheCollectionName, null);
            }
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
            if (cacheItem != null)
                UpdateExpirationTime(set, cacheItem);
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

        public void Set<TValue>(string key, TValue value)
        {
            Add(key, value);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                var item = GetCacheItem(set, key);

                if (item != null)
                {
                    // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                    value = item.Value is TValue ? (TValue)item.Value : default(TValue);
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

        public void Clear(string key)
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                set.Delete(key);
            }
        }

        public void ClearAll()
        {
            using (var context = GetContext())
            {
                var set = GetEntitySet(context);
                set.Delete(i => true);
            }
        }
    }
}
