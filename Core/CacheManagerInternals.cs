using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    // TODO: Create interface(s)
    /// <summary>
    /// The main logic class to handle all the named caches, connectionStrings and notifiers.
    /// The class exist to allow slight decouple from the singleton instance,
    /// Allowing to make the CacheManager to behave differently based on settings, e.g. source of cache configuration
    /// </summary>
    internal class CacheManagerInternals
    {
        private Func<MethodBase> callingMethodGetter;

        // ReSharper disable once InconsistentNaming
        private readonly ConcurrentDictionary<CacheName, ICache> caches
            = new ConcurrentDictionary<CacheName, ICache>();

        // ReSharper disable once InconsistentNaming
        private readonly ConcurrentDictionary<string, ICacheNotifier> notifiers
            = new ConcurrentDictionary<string, ICacheNotifier>();

        // ReSharper disable once InconsistentNaming
        private readonly ConcurrentDictionary<string, ICacheConnectionString> connectionStrings
            = new ConcurrentDictionary<string, ICacheConnectionString>();

        // ReSharper disable once InconsistentNaming
        private readonly ConcurrentDictionary<string, string> cacheNotifierAssociations
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// The source to load the cache configuration from
        /// OPTIONAL: if null then no cache configuration will be loaded
        /// </summary>
        public CacheManagerSettings Settings { get; }

        /// <summary>
        /// The CTOR to set up the initial settings for the cache manager.
        /// OPTIONAL: if null then no cache configuration will be loaded
        /// </summary>
        /// <param name="settings"></param>
        public CacheManagerInternals(CacheManagerSettings settings)
        {
            Settings = settings;
        }

        #region Cache access API

        /// <summary>Gets a cache instance using full name of calling method's class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICache GetCurrentClassCache()
        {
            var method = GetCallingMethod();
            var declaringType = method.DeclaringType;
            return GetCache(declaringType);
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICache GetCache<TClass>()
        {
            return GetCache(typeof(TClass));
        }

        /// <summary>Gets a scoped cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IScopedCache GetScopedCache<TClass>()
        {
            return GetScopedCache(typeof(TClass));
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICache GetCache(Type type)
        {
            return GetCache(type.FullName);
        }

        /// <summary>Gets a scoped cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IScopedCache GetScopedCache(Type type)
        {
            return GetScopedCache(type.FullName);
        }

        /// <summary>Gets a list of all cache names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IEnumerable<string> GetCacheNames()
        {
            var names = caches.Values.ToArray().Select(v => v.Name).ToArray();
            return names;
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICache GetCache(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var cachesArray = GetCaches();

            var cachesSorted = cachesArray.OrderByDescending(c => c.Key.GetMatchLevel(name));
            var cache = cachesSorted.FirstOrDefault();

            return (cache.Key.Prefix != null && cache.Key.GetMatchLevel(name) >= cache.Key.Prefix.Length) ? cache.Value : null;
        }

        /// <summary>Gets a scoped cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IScopedCache GetScopedCache(string name)
        {
            var cache = GetCache(name);
            if (cache is IScopedCache scopedCache)
                return scopedCache;

            throw new ApplicationException($"Cache {name} does not implement IScopedCache");
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        [Obsolete("Use GetCache(string name) instead - no need for async here", true)]
#pragma warning disable 1998
        public async Task<ICache> GetCacheAsync(string name)
#pragma warning restore 1998
        {
            return GetCache(name);
        }

        /// <summary>Gets a cache by name - return a specialized cache implementation type</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public TCache GetCache<TCache>(string name) where TCache : ICache
        {
            var cache = GetCache(name);
            if (!(cache is TCache))
                throw new ArgumentException("The specified cache is not of type " + typeof(TCache));

            return (TCache)cache;
        }

        /// <summary>Gets a list of all notifier names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IEnumerable<string> GetNotifierNames()
        {
            var names = notifiers.Values.ToArray().Select(v => v.Name).ToArray();
            return names;
        }

        /// <summary>Gets a notifier by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICacheNotifier GetNotifier(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            notifiers.TryGetValue(name, out var result);
            return result;
        }

        /// <summary>Gets a list of all connection string names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public IEnumerable<string> GetConnectionStringNames()
        {
            var names = connectionStrings.Values.ToArray().Select(v => v.Name).ToArray();
            return names;
        }

        /// <summary>Gets a connection string by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public ICacheConnectionString GetConnectionString(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            connectionStrings.TryGetValue(name, out var result);
            return result;
        }

        /// <summary>Associates a cache with a notifier</summary>
        /// <param name="cache"></param>
        /// <param name="notifier"></param>
        public void Associate(ICache cache, ICacheNotifier notifier)
        {
            cacheNotifierAssociations.AddOrUpdate(
                cache.Name, c => notifier.Name, (c, n) => notifier.Name);
        }

        /// <summary>
        /// Removes an association between a cache and a notifier.
        /// This method is to be used during application initialization.
        /// </summary>
        public void RemoveAssociation(ICache cache)
        {
            cacheNotifierAssociations.TryRemove(cache.Name, out _);
        }

        /// <summary>
        /// Removes all associations between caches and notifiers.
        /// This method is to be used during application initialization.
        /// </summary>
        public void RemoveAllAssociations()
        {
            cacheNotifierAssociations.Clear();
        }

        /// <summary>Gets the notifier that was associated with a cache</summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public ICacheNotifier GetAssociatedNotifier(ICache cache)
        {
            return cacheNotifierAssociations.TryGetValue(cache.Name, out string notifierName)
                ? GetNotifier(notifierName)
                : null;
        }

        #endregion

        private KeyValuePair<CacheName, ICache>[] GetCaches()
        {
            var cachesArray = caches.ToArray();
            return cachesArray;
        }

        #region Cache configuration API

        /// <summary>
        /// Sets named cached according to set config loader
        /// Initialization can be overriden using SetCache and RemoveCache (and this method)
        /// </summary>
        public void InitializeFromConfig()
        {
            RemoveAllAssociations();
            RemoveAllCaches();
            RemoveAllNotifiers();
            RemoveAllConnectionStrings();

            var configLoader = Settings?.ConfigLoader;
            if (configLoader != null)
            {
                var config = configLoader.LoadConfig();
                ApplyConfig(config);

                if (Settings.ShouldRegisterAllCaches)
                {
                    var ccu = new CacheControllerUtil();
                    ccu.ClearRegisteredCacheNames();
                    ccu.RegisterAllCaches();
                }
            }
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cache"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public void SetCache(string name, ICache cache)
        {
            var cacheName = new CacheName(name);
            if (cache == null)
                caches.TryRemove(cacheName, out var _);
            else
                caches.AddOrUpdate(cacheName, cache, (k, v) => cache);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public void RemoveCache(string name)
        {
            var cacheName = new CacheName(name);
            caches.TryRemove(cacheName, out var _);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public void RemoveAllCaches()
        {
            caches.Clear();
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectionString"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public void SetConnectionString(string name, ICacheConnectionString connectionString)
        {
            if (connectionString == null)
                connectionStrings.TryRemove(name, out var _);
            else
                connectionStrings.AddOrUpdate(name, connectionString, (k, v) => connectionString);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public void RemoveConnectionString(string name)
        {
            connectionStrings.TryRemove(name, out var _);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public void RemoveAllConnectionStrings()
        {
            connectionStrings.Clear();
        }

        /// <summary>
        /// Adds or sets a notifier by name
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="notifier"></param>
        public void SetNotifier(string name, ICacheNotifier notifier)
        {
            if (notifier == null)
                notifiers.TryRemove(name, out var _);
            else
                notifiers.AddOrUpdate(name, notifier, (k, v) => notifier);
        }

        /// <summary>
        /// Removes a notifier registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        public void RemoveNotifier(string name)
        {
            notifiers.TryRemove(name, out var _);
        }

        /// <summary>
        /// Removes all notifier registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        public void RemoveAllNotifiers()
        {
            notifiers.Clear();
        }

        #endregion

        #region Automatic cache naming

        // ReSharper disable once InconsistentNaming
        private static readonly object loadLock = new object();

        private MethodBase GetCallingMethod()
        {
            Func<MethodBase> method = callingMethodGetter;
            if (method == null)
            {
                lock (loadLock)
                {
                    if (callingMethodGetter == null)
                        callingMethodGetter = CreateGetClassNameFunction();

                    method = callingMethodGetter;
                }
            }
            return method();
        }

        private Func<MethodBase> CreateGetClassNameFunction()
        {
            var stackFrameType = Type.GetType("System.Diagnostics.StackFrame");
            if (stackFrameType == null)
                throw new PlatformNotSupportedException("CreateGetClassNameFunction is only supported on platforms where System.Diagnostics.StackFrame exist");

            var constructor = stackFrameType.GetConstructor(new[] { typeof(int) });
            var getMethodMethod = stackFrameType.GetMethod("GetMethod");

            if (constructor == null)
                throw new PlatformNotSupportedException("StackFrame(int skipFrames) constructor not present");

            if (getMethodMethod == null)
                throw new PlatformNotSupportedException("StackFrame.GetMethod() not present");

            var stackFrame = Expression.New(constructor, Expression.Constant(4));
            var method = Expression.Call(stackFrame, getMethodMethod);
            var lambda = Expression.Lambda<Func<MethodBase>>(method);
            return lambda.Compile();
        }

        #endregion

        #region Config Loading

        private void ApplyConfig(IList<ConfigNode> config)
        {
            if (config == null)
                return;

            var connectionConfigs = new List<ConnectionStringConfig>();
            var notifierConfigs = new List<NotifierConfig>();
            var cacheConfigs = new List<CacheConfig>();
            var connectionRemoveIndexes = new List<int>();
            var notifierRemoveIndexes = new List<int>();
            var cacheRemoveIndexes = new List<int>();

            foreach (var item in config)
            {
                switch (item.Action)
                {
                    case ConfigAction.Remove:
                        // Remove existing nodes
                        RemoveCache(item.Name);
                        RemoveNotifier(item.Name);
                        RemoveConnectionString(item.Name);

                        // Save which pending nodes to remove
                        connectionRemoveIndexes.AddRange(
                            connectionConfigs.Select((cfg, index) => Tuple.Create(index, cfg))
                                .Where(c => c.Item2.Name == item.Name).Select(c => c.Item1).Reverse());
                        notifierRemoveIndexes.AddRange(
                            notifierConfigs.Select((cfg, index) => Tuple.Create(index, cfg))
                                .Where(c => c.Item2.Name == item.Name).Select(c => c.Item1).Reverse());
                        cacheRemoveIndexes.AddRange(
                            cacheConfigs.Select((cfg, index) => Tuple.Create(index, cfg))
                                .Where(c => c.Item2.Name == item.Name).Select(c => c.Item1).Reverse());
                        break;

                    case ConfigAction.Add:
                        // Add to pending nodes
                        if (item is CacheConfig cacheConfig)
                        {
                            cacheConfigs.Add(cacheConfig);
                        }
                        else if (item is NotifierConfig notifierConfig)
                        {
                            notifierConfigs.Add(notifierConfig);
                        }
                        else if (item is ConnectionStringConfig connectionStringConfig)
                        {
                            connectionConfigs.Add(connectionStringConfig);
                        }
                        break;
                }
            }

            // Remove pending nodes marked for removal
            cacheRemoveIndexes = cacheRemoveIndexes.OrderByDescending(i => i).ToList();
            notifierRemoveIndexes = notifierRemoveIndexes.OrderByDescending(i => i).ToList();
            connectionRemoveIndexes = connectionRemoveIndexes.OrderByDescending(i => i).ToList();
            foreach (var i in cacheRemoveIndexes)
                cacheConfigs.RemoveAt(i);
            foreach (var i in notifierRemoveIndexes)
                notifierConfigs.RemoveAt(i);
            foreach (var i in connectionRemoveIndexes)
                connectionConfigs.RemoveAt(i);

            // Add still pending nodes,
            // order by types (to enable forward declaration)
            // and then by appearance in config
            // TODO: Pass ICacheManager or subset to the Create*() that call CacheManager directly
            foreach (var item in connectionConfigs)
                SetConnectionString(item.Name, item.CreateConnectionString());
            foreach (var item in notifierConfigs)
                SetNotifier(item.Name, item.CreateCacheNotifier());
            foreach (var item in cacheConfigs.OrderBy(c => c.LoadPriority))
                SetCache(item.Name, item.CreateCache());
        }

        #endregion
    }
}
