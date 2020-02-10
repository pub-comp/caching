using PubComp.Caching.Core.Exceptions;
using PubComp.Caching.Core.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PubComp.Caching.Core
{
    public class CacheControllerUtil
    {
        private static readonly ConcurrentDictionary<string, bool> RegisteredCacheNames;
        private static readonly ConcurrentDictionary<Tuple<string, string>, Func<object>> RegisteredCacheItems;

        static CacheControllerUtil()
        {
            RegisteredCacheNames = new ConcurrentDictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
            RegisteredCacheItems = new ConcurrentDictionary<Tuple<string, string>, Func<object>>();
        }

        /// <summary>
        /// Clears all data from a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        public void ClearCache(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache not cleared - received undefined cacheName");
            }

            var cache = CacheManager.GetCache(cacheName);
            
            if (cache == null)
            {
                throw new CacheClearException("Cache not cleared - cache not found: " + cacheName);
            }

            if (!cache.Name.Equals(cacheName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new CacheClearException("Cache not cleared - due to fallback to a general cache: " + cacheName);
            }

            bool doEnableClear;
            if (RegisteredCacheNames.TryGetValue(cacheName, out doEnableClear))
            {
                if (!doEnableClear)
                {
                    throw new CacheClearException("Cache not cleared - cache registered with doEnableClearEntireCache=False");
                }
            }

            cache.ClearAll();

            var notifier = CacheManager.GetAssociatedNotifier(cache);
            if (notifier != null)
                notifier.Publish(cache.Name, null, CacheItemActionTypes.RemoveAll);
        }

        /// <summary>
        /// Clears a specific cache item in a named cache instance by key
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        public void ClearCacheItem(string cacheName, string itemKey)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache item not cleared - received undefined cacheName");
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                throw new CacheClearException("Cache item not cleared - received undefined itemKey");
            }

            var cache = CacheManager.GetCache(cacheName);

            if (cache == null)
            {
                throw new CacheClearException("Cache item not cleared - cache not found: " + cacheName);
            }

            if (!cache.Name.Equals(cacheName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new CacheClearException("Cache not cleared - due to fallback to a general cache: " + cacheName);
            }

            cache.Clear(itemKey);

            var notifier = CacheManager.GetAssociatedNotifier(cache);
            if (notifier != null)
                notifier.Publish(cacheName, itemKey, CacheItemActionTypes.Removed);
        }

        /// <summary>
        /// Registers all defined caches
        /// </summary>
        public void RegisterAllCaches()
        {
            var caches = CacheManager.GetCacheNames();

            foreach (var cache in caches)
            {
                RegisterCache(cache, true);
            }
        }

        /// <summary>
        /// Register a named cache instance for remote clear access via controller
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="doEnableClearEntireCache"></param>
        public void RegisterCache(string cacheName, bool doEnableClearEntireCache)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache not registered - received undefined cacheName");
            }

            RegisteredCacheNames.AddOrUpdate(
                cacheName,
                name => doEnableClearEntireCache,
                (name, existingValue) => doEnableClearEntireCache);
        }

        /// <summary>
        /// Register a cache item for remote clear/refresh access via controller
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="getterExpression"></param>
        /// <param name="doInitialize"></param>
        public void RegisterCacheItem<TItem>(Expression<Func<TItem>> getterExpression, bool doInitialize)
            where TItem : class
        {
            if (getterExpression == null)
            {
                throw new CacheClearException("Cache item not registered - received undefined getterExpression");
            }

            LambdaHelper.GetMethodInfoAndArguments(getterExpression, out var method, out _);

            // CacheListAttribute is intentionally not supported, as cacheItemKey vary according to input
            // these should be dealt with by using a dedicated cache and clearing this entire dedicated cache
            var cacheName = method.GetCustomAttributesData()
                .Where(a =>
                    a.AttributeType.FullName == "PubComp.Caching.AopCaching.CacheAttribute"
                    && a.ConstructorArguments.Any()
                    && a.ConstructorArguments.First().ArgumentType == typeof(string))
                .Select(a => (a.ConstructorArguments.First().Value ?? string.Empty).ToString())
                .FirstOrDefault();

            var methodType = method.DeclaringType;

            if (methodType == null)
            {
                throw new CacheClearException("Cache item not registered - invalid getterExpression");
            }

            if (cacheName == null)
                cacheName = methodType.FullName;

            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache item not registered - received undefined cacheName");
            }

            var itemKey = CacheKey.GetKey(getterExpression);

            if (string.IsNullOrEmpty(itemKey))
            {
                throw new CacheClearException("Cache item not registered - received undefined itemKey");
            }

            RegisteredCacheNames.GetOrAdd(cacheName, false);

            var getter = getterExpression.Compile();

            Func<Tuple<string, string>, Func<object>, Func<object>> updateGetter
                = (k, o) => getter;

            RegisteredCacheItems.AddOrUpdate(
                Tuple.Create(cacheName, itemKey), getter, updateGetter);

            if (doInitialize)
            {
                if (!TrySetCacheItem(cacheName, itemKey, getter))
                {
                    throw new CacheClearException("Cache item not initialized - cache not defined: " + cacheName);
                }
            }
        }

        /// <summary>
        /// Register a cache item for remote clear access via controller
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        protected void RegisterCacheItem<TItem>(string cacheName, string itemKey)
            where TItem : class
        {
            RegisterCacheItem<TItem>(cacheName, itemKey, null, false);
        }

        /// <summary>
        /// Register a cache item for remote clear/refresh access via controller
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        /// <param name="getter"></param>
        /// <param name="doInitialize"></param>
        protected void RegisterCacheItem<TItem>(
            string cacheName, string itemKey, Func<TItem> getter, bool doInitialize)
            where TItem : class
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache item not registered - received undefined cacheName");
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                throw new CacheClearException("Cache item not registered - received undefined itemKey");
            }

            RegisteredCacheNames.GetOrAdd(cacheName, false);

            Func<Tuple<string, string>, Func<object>, Func<object>> updateGetter
                = (k, o) => getter;

            RegisteredCacheItems.AddOrUpdate(
                Tuple.Create(cacheName, itemKey), getter, updateGetter);

            if (doInitialize)
            {
                if (!TrySetCacheItem(cacheName, itemKey, getter))
                {
                    throw new CacheClearException("Cache item not initialized - cache not defined: " + cacheName);
                }
            }
        }

        /// <summary>
        /// Clears all registrations (of named caches and cache items for remote clear/refresh access via controller)
        /// </summary>
        protected void ClearRegistrations()
        {
            RegisteredCacheItems.Clear();
            RegisteredCacheNames.Clear();
        }

        /// <summary>
        /// Clears all registrations of named caches
        /// </summary>
        internal void ClearRegisteredCacheNames()
        {
            RegisteredCacheNames.Clear();
        }

        /// <summary>
        /// Gets names of all registered cache instances
        /// </summary>
        public IEnumerable<string> GetRegisteredCacheNames()
        {
            return RegisteredCacheNames.Keys.ToList();
        }

        /// <summary>
        /// Gets keys of all registered cache items a specific in a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        public IEnumerable<string> GetRegisteredCacheItemKeys(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Received undefined cacheName");
            }

            return RegisteredCacheItems.Keys.ToList()
                .Where(k => k.Item1 == cacheName)
                .Select(k => k.Item2)
                .ToList();
        }

        /// <summary>
        /// Refreshes a specific registered cache item in a named cache instance by key
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        public void RefreshCacheItem(string cacheName, string itemKey)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new CacheClearException("Cache item not refreshed - received undefined cacheName");
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                throw new CacheClearException("Cache item not refreshed - received undefined itemKey");
            }

            Func<object> registeredGetter;

            if (RegisteredCacheItems.TryGetValue(Tuple.Create(cacheName, itemKey), out registeredGetter))
            {
                if (registeredGetter == null)
                {
                    throw new CacheClearException(string.Concat(
                        "Cache item not refreshed - getter not defined: ", cacheName, "/", itemKey));
                }

                if (!TrySetCacheItem(cacheName, itemKey, registeredGetter))
                {
                    throw new CacheClearException("Cache item not refresh - cache not defined: " + cacheName);
                }

                return;
            }

            throw new CacheClearException(string.Concat(
                "Cache item not refreshed - item is not registered: ", cacheName, "/", itemKey));
        }

        private bool TrySetCacheItem(string cacheName, string itemKey, Func<object> getter)
        {
            var cache = CacheManager.GetCache(cacheName);
            if (cache == null)
            {
                return false;
            }

            cache.Set(itemKey, getter());

            var notifier = CacheManager.GetAssociatedNotifier(cache);
            if (notifier != null)
                notifier.Publish(cacheName, itemKey, CacheItemActionTypes.Removed);

            return true;
        }
    }
}
