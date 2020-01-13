using PubComp.Caching.Core;
using PubComp.Caching.Core.CacheUtils;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.SystemRuntime
{
    public abstract class ObjectCache : ICache
    {
        private readonly String name;
        private System.Runtime.Caching.ObjectCache innerCache;
        
        protected readonly MultiLock Locks;
        protected readonly InMemoryPolicy Policy;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly string notiferName;

        // ReSharper disable once NotAccessedField.Local - reference isn't necessary, but it makes debugging easier
        private readonly CacheSynchronizer synchronizer;

        protected ObjectCache(
            String name, System.Runtime.Caching.ObjectCache innerCache, InMemoryPolicy policy)
        {
            this.name = name;
            this.Policy = policy;
            this.innerCache = innerCache;

            this.Locks = !this.Policy.DoNotLock
                ? new MultiLock(
                    this.Policy.NumberOfLocks ?? 50,
                    this.Policy.LockTimeoutMilliseconds != null && this.Policy.LockTimeoutMilliseconds > 0
                        ? this.Policy.LockTimeoutMilliseconds
                        : null,
                    this.Policy.DoThrowExceptionOnTimeout ?? true)
                : null;

            this.notiferName = this.Policy?.SyncProvider;
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.notiferName);
        }

        public string Name { get { return this.name; } }

        protected System.Runtime.Caching.ObjectCache InnerCache { get { return this.innerCache; } }

        protected InMemoryExpirationPolicy ExpirationPolicy
        {
            get
            {
                if (synchronizer?.IsActive ?? true)
                {
                    return Policy;
                }
                else
                {
                    return Policy.OnSyncProviderFailure ?? Policy;
                }
            }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner(key, out value);
        }

        public Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return Task.FromResult(new TryGetResult<TValue>
            {
                WasFound = TryGetInner<TValue>(key, out var value),
                Value = value
            });
        }

        public void Set<TValue>(string key, TValue value)
        {
            Add(key, value);
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            Set(key, value);
            return Task.FromResult<object>(null);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            if (innerCache.Get(key, null) is CacheItem item)
            {
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                // ReSharper disable once MergeConditionalExpression
                value = item.Value is TValue ? (TValue)item.Value : default(TValue);
                return true;
            }

            value = default(TValue);
            return false;
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            innerCache.Set(key, new CacheItem(value), GetRuntimePolicy(), null);
        }

        // ReSharper disable once ParameterHidesMember
        protected System.Runtime.Caching.CacheItemPolicy GetRuntimePolicy()
            => ToRuntimePolicy(ExpirationPolicy);

        // ReSharper disable once ParameterHidesMember
        protected System.Runtime.Caching.CacheItemPolicy ToRuntimePolicy(InMemoryPolicy policy) 
            => ToRuntimePolicy((InMemoryExpirationPolicy)policy);
        
        // ReSharper disable once ParameterHidesMember
        protected System.Runtime.Caching.CacheItemPolicy ToRuntimePolicy(InMemoryExpirationPolicy expirationPolicy)
        {
            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;

            if (expirationPolicy.SlidingExpiration != null && expirationPolicy.SlidingExpiration.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
                slidingExpiration = expirationPolicy.SlidingExpiration.Value;
            }
            else if (expirationPolicy.ExpirationFromAdd != null && expirationPolicy.ExpirationFromAdd.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = DateTimeOffset.Now.Add(expirationPolicy.ExpirationFromAdd.Value);
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }
            else if (expirationPolicy.AbsoluteExpiration != null && expirationPolicy.AbsoluteExpiration.Value < DateTimeOffset.MaxValue)
            {
                absoluteExpiration = expirationPolicy.AbsoluteExpiration.Value;
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }
            else
            {
                absoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }

            return new System.Runtime.Caching.CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };
        }

        public virtual TValue Get<TValue>(String key, Func<TValue> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            TValue OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                value = getter();
                Set(key, value);
                return value;
            }

            if (Policy.DoNotLock)
                return OnCacheMiss();

            return this.Locks.LockAndLoad(key, OnCacheMiss);
        }

        public virtual async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            async Task<TValue> OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                value = await getter().ConfigureAwait(false);
                Set(key, value);
                return value;
            }

            if (Policy.DoNotLock)
                return await OnCacheMiss().ConfigureAwait(false);

            return await this.Locks.LockAndLoadAsync(key, OnCacheMiss).ConfigureAwait(false);
        }

        public void Clear(String key)
        {
            innerCache.Remove(key, null);
        }

        public Task ClearAsync(string key)
        {
            innerCache.Remove(key, null);
            return Task.FromResult<object>(null);
        }

        public void ClearAll()
        {
            innerCache = new System.Runtime.Caching.MemoryCache(this.name);
        }

        public Task ClearAllAsync()
        {
            innerCache = new System.Runtime.Caching.MemoryCache(this.name);
            return Task.FromResult<object>(null);
        }
    }
}
