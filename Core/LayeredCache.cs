using PubComp.Caching.Core.Notifications;
using System;
using System.Threading.Tasks;
// ReSharper disable NotAccessedField.Local
// ReSharper disable UseStringInterpolation

namespace PubComp.Caching.Core
{
    /// <summary>
    /// A layered cache e.g. level1 = in-memory cache that falls back to level2 = distributed cache
    /// </summary>
    public class LayeredCache : ICache
    {
        private readonly String name;
        private ICache level1;
        private ICache level2;
        private readonly LayeredCachePolicy policy;
        private readonly CacheSynchronizer synchronizer;
        private readonly ICacheNotifier level1Notifier;

        public LayeredCache(String name, LayeredCachePolicy policy)
            : this(name, policy?.Level1CacheName, policy?.Level2CacheName)
        {
            this.policy = policy;

            if (policy?.InvalidateLevel1OnLevel2Update ?? false)
            {
                level1Notifier = CacheManager.GetAssociatedNotifier(this.level1);
                if (level1Notifier == null)
                    throw new ApplicationException("InvalidateLevel1OnLevel2Update requires level1 cache to have SyncProvider defined in policy: level1CacheName=" + policy.Level1CacheName);
            }
        }

        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1CacheName">Name of first cache to check (e.g. in-memory cache), should be registered in CacheManager</param>
        /// <param name="level2CacheName">Name of fallback cache (e.g. distributed cache), should be registered in CacheManager</param>
        public LayeredCache(String name, String level1CacheName, String level2CacheName)
        {
            this.name = name;

            // ReSharper disable once LocalVariableHidesMember
            var level1 = CacheManager.GetCache(level1CacheName);
            if (level1 == null)
                throw new ApplicationException("Cache is not registered: level1CacheName=" + level1CacheName);

            // ReSharper disable once LocalVariableHidesMember
            var level2 = CacheManager.GetCache(level2CacheName);
            if (level2 == null)
                throw new ApplicationException("Cache is not registered: level2CacheName=" + level2CacheName);

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("level2 must not be the same as level1, received {0}={1}, {2}={3}, which map to {4} and {5}",
                        "level1CacheName", level1CacheName, "level2CacheName", level2CacheName, level1.Name, level2.Name));
            }

            this.level1 = level1;
            this.level2 = level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1CacheName, Level2CacheName = level1CacheName };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy?.SyncProvider);
        }

        /// <summary>
        /// Creates a layered cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level1">First cache to check (e.g. in-memory cache)</param>
        /// <param name="level2">Fallback cache (e.g. distributed cache)</param>
        public LayeredCache(String name, ICache level1, ICache level2)
        {
            this.name = name;

            if (level1 == null)
                throw new ApplicationException("innerCache1 must not be null");

            if (level2 == null)
                throw new ApplicationException("innerCache2 must not be null");

            if (level2 == level1)
            {
                throw new ApplicationException(
                    string.Format("Cache2 must not be the same as cache2, received {0}={1} and {2}={3}",
                        "level1", level1.Name, "level2", level2.Name));
            }

            this.level1 = level1;
            this.level2 = level2;

            this.policy = new LayeredCachePolicy { Level1CacheName = level1.Name, Level2CacheName = level2.Name };
            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy?.SyncProvider);
        }

        public string Name { get { return this.name; } }

        protected ICache Level1 { get { return this.level1; } }

        protected ICache Level2 { get { return this.level2; } }

        protected LayeredCachePolicy Policy { get { return this.policy; } }
        
        public bool TryGet<TValue>(string key, out TValue value)
        {
            if (this.level1.TryGet(key, out value))
                return true;

            if (this.level2.TryGet(key, out value))
            {
                this.level1.Set(key, value);
                return true;
            }

            value = default;
            return false;
        }

        public async Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            var level1Result = await this.level1.TryGetAsync<TValue>(key).ConfigureAwait(false);
            if (level1Result.WasFound)
                return level1Result;

            var level2Result = await this.level2.TryGetAsync<TValue>(key).ConfigureAwait(false);
            if (level2Result.WasFound)
            {
                this.level1.Set(key, level2Result.Value);
                return level2Result;
            }

            return new TryGetResult<TValue> {WasFound = false};
        }

        public void Set<TValue>(String key, TValue value)
        {
            this.level2.Set(key, value);
            this.level1.Set(key, value);

            if (this.policy.InvalidateLevel1OnLevel2Update)
                level1Notifier.Publish(policy.Level1CacheName, key, CacheItemActionTypes.Updated);
        }

        public async Task SetAsync<TValue>(string key, TValue value)
        {
            await this.level2.SetAsync(key, value).ConfigureAwait(false);
            await this.level1.SetAsync(key, value).ConfigureAwait(false);

            if (this.policy.InvalidateLevel1OnLevel2Update)
                await level1Notifier
                    .PublishAsync(policy.Level1CacheName, key, CacheItemActionTypes.Updated)
                    .ConfigureAwait(false);
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            var getterHasBeenInvoked = false;
            var value =
                this.level1.Get(key, () =>
                    this.level2.Get(key, () =>
                    {
                        getterHasBeenInvoked = true;
                        return getter();
                    }));

            if (getterHasBeenInvoked && this.policy.InvalidateLevel1OnLevel2Update)
                this.level1Notifier.Publish(policy.Level1CacheName, key, CacheItemActionTypes.Updated);

            return value;
        }

        public async Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            var getterHasBeenInvoked = false;
            var value =
                await this.level1.GetAsync(key, async () =>
                        await this.level2.GetAsync(key, async () =>
                        {
                            getterHasBeenInvoked = true;
                            return await getter().ConfigureAwait(false);
                        }).ConfigureAwait(false))
                    .ConfigureAwait(false);

            if (getterHasBeenInvoked && this.policy.InvalidateLevel1OnLevel2Update)
                await this.level1Notifier
                    .PublishAsync(policy.Level1CacheName, key, CacheItemActionTypes.Updated)
                    .ConfigureAwait(false);

            return value;
        }

        public void Clear(String key)
        {
            this.level2.Clear(key);
            this.level1.Clear(key);
        }

        public async Task ClearAsync(string key)
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
