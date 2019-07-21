using System;
using System.Threading.Tasks;
using PubComp.Caching.Core;
using PubComp.Caching.Core.CacheUtils;

namespace PubComp.Caching.SystemRuntime
{
    public abstract class ObjectCache : ICache
    {
        private readonly String name;
        private System.Runtime.Caching.ObjectCache innerCache;
        private readonly MultiLock locks;
        private readonly InMemoryPolicy policy;
        
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly string notiferName;

        // ReSharper disable once NotAccessedField.Local - reference isn't necessary, but it makes debugging easier
        private readonly CacheSynchronizer synchronizer;

        protected ObjectCache(
            String name, System.Runtime.Caching.ObjectCache innerCache, InMemoryPolicy policy)
        {
            this.name = name;
            this.policy = policy;
            this.innerCache = innerCache;

            this.locks = !this.policy.DoNotLock
                ? new MultiLock(
                    this.policy.NumberOfLocks ?? 50,
                    this.policy.LockTimeoutMilliseconds != null && this.policy.LockTimeoutMilliseconds > 0
                        ? this.policy.LockTimeoutMilliseconds
                        : null,
                    this.policy.DoThrowExceptionOnTimeout ?? true)
                : null;

            this.notiferName = this.policy?.SyncProvider;
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.notiferName);
        }
        
        public string Name { get { return this.name; } }

        protected System.Runtime.Caching.ObjectCache InnerCache { get { return this.innerCache; } }

        protected InMemoryPolicy Policy { get { return this.policy; } }
        
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
            Add(key, value);
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
            innerCache.Set(key, new CacheItem(value), ToRuntimePolicy(policy), null);
        }

        // ReSharper disable once ParameterHidesMember
        protected System.Runtime.Caching.CacheItemPolicy ToRuntimePolicy(InMemoryPolicy policy)
        {
            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;

            if (policy.SlidingExpiration != null && policy.SlidingExpiration.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
                slidingExpiration = policy.SlidingExpiration.Value;                
            }
            else if (policy.ExpirationFromAdd != null && policy.ExpirationFromAdd.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = DateTimeOffset.Now.Add(policy.ExpirationFromAdd.Value);
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }
            else if (policy.AbsoluteExpiration != null && policy.AbsoluteExpiration.Value < DateTimeOffset.MaxValue)
            {
                absoluteExpiration = policy.AbsoluteExpiration.Value;
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
                SlidingExpiration = slidingExpiration,
            };
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            TValue OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                value = getter();
                Add(key, value);
                return value;
            }

            if (policy.DoNotLock)
                return OnCacheMiss();

            return this.locks.LockAndLoad(key, OnCacheMiss);
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            if (TryGetInner(key, out TValue value))
                return value;

            async Task<TValue> OnCacheMiss()
            {
                if (TryGetInner(key, out value)) return value;

                value = await getter().ConfigureAwait(false);
                Add(key, value);
                return value;
            }

            if (policy.DoNotLock)
                return await OnCacheMiss().ConfigureAwait(false);

            return await this.locks.LockAndLoadAsync(key, OnCacheMiss).ConfigureAwait(false);
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
