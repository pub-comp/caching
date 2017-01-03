using System;
using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisCache : ICache
    {
        private readonly String name;
        private readonly String connectionString;
        private readonly bool useSlidingExpiration;
        private readonly TimeSpan? expireWithin;
        private readonly DateTime? expireAt;
        private readonly string converterType;
        private readonly string clusterType;
        private readonly int monitorPort;
        private readonly int monitorIntervalMilliseconds;
        private readonly CacheContext _cache = null;

        public RedisCache(String name, RedisCachePolicy policy)
        {
            this.name = name;
            LogHelper.Log.Debug("Init Cache {0}", this.name);

            if (policy == null)
            {
                LogHelper.Log.Debug("Invalid Policy for Cache {0}", this.name);
                throw new ArgumentNullException("policy");
            }

            this.connectionString = policy.ConnectionString;
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

            _cache = new CacheContext(this.connectionString, this.converterType, this.clusterType, this.monitorPort, this.monitorIntervalMilliseconds);
        }

        public string Name { get { return this.name; } }

        
        private CacheContext Cache {
            get { return _cache; }
        }

        private TValue GetOrAdd<TValue>(
            CacheContext context, string key, TValue newValue, bool doForceOverride = false)
        {
            var newItem = CreateCacheItem(key, newValue);

            if (context.SetIfNotExists(newItem))
            {
                //if (newItem.ExpireIn.HasValue)
                //    context.SetExpirationTime(newItem);
                return newValue;
            }

            var prevValue = GetCacheItem<TValue>(context, key);
            if (!doForceOverride && prevValue != null && prevValue.Value is TValue)
                return prevValue.Value;

            context.SetItem(newItem);
            //if (newItem.ExpireIn.HasValue)
            //    context.SetExpirationTime(newItem);

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

        private void ResetExpirationTime<TValue>(CacheContext context, CacheItem<TValue> cacheItem)
        {
            if (expireWithin.HasValue && useSlidingExpiration)
            {
                context.SetIfNotExists(cacheItem);
                cacheItem.ExpireIn = this.expireWithin.Value;
                context.SetExpirationTime(cacheItem);
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
            var cacheItem = Cache.GetItem<TValue>(this.Name, key);

            if (cacheItem != null)
            {
                value = cacheItem.Value;
                ResetExpirationTime(Cache, cacheItem);
                return true;
            }

            value = default(TValue);
            return false;
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            GetOrAdd(Cache, key, value, true);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            TValue value;
            if (TryGetInner(key, out value))
                return value;

            value = getter();
            return GetOrAdd(Cache, key, value);
        }

        public void Clear(string key)
        {
            Cache.RemoveItem(this.Name, key);
        }

        public void ClearAll()
        {
            Cache.ClearItems(this.Name);
        }
    }
}
