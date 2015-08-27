using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PubComp.Caching.Core
{
    public class CacheControllerUtil
    {
        private readonly Action<string> logInfo;
        private readonly Action<string> logWarning;
        private readonly ConcurrentDictionary<string, bool> registeredCacheNames;
        private readonly ConcurrentDictionary<Tuple<string, string>, Func<object>> registeredCacheItems;

        public CacheControllerUtil(Action<string> logInfo, Action<string> logWarning)
        {
            this.logInfo = logInfo;
            this.logWarning = logWarning;
            this.registeredCacheNames = new ConcurrentDictionary<string, bool>();
            this.registeredCacheItems = new ConcurrentDictionary<Tuple<string, string>, Func<object>>();
        }

        /// <summary>
        /// Clears all data from a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        public void ClearCache(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                LogWarning("Cache not cleared - received undefined cacheName");
                return;
            }

            var cache = CacheManager.GetCache(cacheName);
            
            if (cache == null)
            {
                LogWarning("Cache not cleared - cache not found: " + cacheName);
                return;
            }

            if (cache.Name != cacheName)
            {
                LogWarning("Cache not cleared - due to fallback to a general cache: " + cacheName);
                return;
            }

            cache.ClearAll();
            logInfo("Cache cleared: " + cacheName);
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
                LogWarning("Cache item not cleared - received undefined cacheName");
                return;
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                LogWarning("Cache item not cleared - received undefined itemKey");
                return;
            }

            var cache = CacheManager.GetCache(cacheName);

            if (cache == null)
            {
                LogWarning("Cache item not cleared - cache not found: " + cacheName);
                return;
            }

            cache.Clear(itemKey);
            LogInfo(string.Concat("Cache item cleared: ", cacheName, "/", itemKey));
        }

        /// <summary>
        /// Register a named cache instance for remote clear access via controller
        /// </summary>
        /// <param name="cacheName"></param>
        public void RegisterCache(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                LogWarning("Cache not registered - received undefined cacheName");
                return;
            }

            this.registeredCacheNames.GetOrAdd(cacheName, true);
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
                LogWarning("Cache item not registered - received undefined getterExpression");
                return;
            }

            MethodInfo method;
            object[] arguments;
            LambdaHelper.GetMethodInfoAndArguments(getterExpression, out method, out arguments);

            var cacheName = method.GetCustomAttributesData()
                .Where(a =>
                    (a.AttributeType.FullName == "PubComp.Caching.AopCaching.CacheAttribute"
                    || a.AttributeType.FullName == "PubComp.Caching.AopCaching.CacheAttribute")
                    && a.ConstructorArguments.Any()
                    && a.ConstructorArguments.First().ArgumentType == typeof(string))
                .Select(a => (a.ConstructorArguments.First().Value ?? string.Empty).ToString())
                .FirstOrDefault();

            if (cacheName == null)
                cacheName = method.DeclaringType.FullName;

            if (string.IsNullOrEmpty(cacheName))
            {
                LogWarning("Cache item not registered - received undefined cacheName");
                return;
            }

            var itemKey = CacheKey.GetKey(getterExpression);

            if (string.IsNullOrEmpty(itemKey))
            {
                LogWarning("Cache item not registered - received undefined itemKey");
                return;
            }

            RegisterCache(cacheName);

            var getter = getterExpression.Compile();

            Func<Tuple<string, string>, Func<object>, Func<object>> updateGetter
                = (k, o) => getter;

            this.registeredCacheItems.AddOrUpdate(
                Tuple.Create(cacheName, itemKey), getter, updateGetter);

            if (doInitialize)
            {
                if (!TrySetCacheItem(cacheName, itemKey, getter))
                {
                    LogWarning("Cache item not initialized - cache not defined: " + cacheName);
                    return;
                }

                LogInfo(string.Concat("Cache item initialized: ", cacheName, "/", itemKey));
            }
        }

        /// <summary>
        /// Register a cache item for remote clear access via controller
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        public void RegisterCacheItem<TItem>(string cacheName, string itemKey)
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
        public void RegisterCacheItem<TItem>(
            string cacheName, string itemKey, Func<TItem> getter, bool doInitialize)
            where TItem : class
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                LogWarning("Cache item not registered - received undefined cacheName");
                return;
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                LogWarning("Cache item not registered - received undefined itemKey");
                return;
            }

            RegisterCache(cacheName);

            Func<Tuple<string, string>, Func<object>, Func<object>> updateGetter
                = (k, o) => getter;

            this.registeredCacheItems.AddOrUpdate(
                Tuple.Create(cacheName, itemKey), getter, updateGetter);

            if (doInitialize)
            {
                if (!TrySetCacheItem(cacheName, itemKey, getter))
                {
                    LogWarning("Cache item not initialized - cache not defined: " + cacheName);
                    return;
                }

                LogInfo(string.Concat("Cache item initialized: ", cacheName, "/", itemKey));
            }
        }

        /// <summary>
        /// Gets names of all registered cache instances
        /// </summary>
        public IEnumerable<string> GetRegisteredCacheNames()
        {
            return this.registeredCacheNames.Keys.ToList();
        }

        /// <summary>
        /// Gets keys of all registered cache items a specific in a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        public IEnumerable<string> GetRegisteredCacheItemKeys(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                LogWarning("Received undefined cacheName");
                return new string[0];
            }

            return this.registeredCacheItems.Keys.ToList()
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
                LogWarning("Cache item not refreshed - received undefined cacheName");
                return;
            }

            if (string.IsNullOrEmpty(itemKey))
            {
                LogWarning("Cache item not refreshed - received undefined itemKey");
                return;
            }

            Func<object> registeredGetter;

            if (this.registeredCacheItems.TryGetValue(Tuple.Create(cacheName, itemKey), out registeredGetter))
            {
                if (registeredGetter == null)
                {
                    LogWarning(string.Concat(
                        "Cache item not refreshed - getter not defined: ", cacheName, "/", itemKey));
                    return;
                }

                if (!TrySetCacheItem(cacheName, itemKey, registeredGetter))
                {
                    LogWarning("Cache item not refresh - cache not defined: " + cacheName);
                    return;
                }

                LogInfo(string.Concat("Cache item refreshed: ", cacheName, "/", itemKey));
                return;
            }

            LogWarning(string.Concat(
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
            return true;
        }

        private void LogInfo(string message)
        {
            if (this.logInfo != null)
                this.logInfo(message);
        }

        private void LogWarning(string message)
        {
            if (this.logWarning != null)
                this.logWarning(message);
        }
    }
}
