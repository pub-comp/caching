using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.Core
{
    public class CacheManager
    {
        private static Func<MethodBase> callingMethodGetter;

        //ReSharper disable once InconsistentNaming
        private static readonly SemaphoreSlim cachesSync
            = new SemaphoreSlim(1, 1);

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentDictionary<CacheName, ICache> caches
            = new ConcurrentDictionary<CacheName, ICache>();

        // ReSharper disable once InconsistentNaming
        private static readonly ReaderWriterLockSlim notifiersSync
            = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentDictionary<string, ICacheNotifier> notifiers
            = new ConcurrentDictionary<string, ICacheNotifier>();

        // ReSharper disable once InconsistentNaming
        private static readonly ReaderWriterLockSlim connectionStringsSync
            = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentDictionary<string, ICacheConnectionString> connectionStrings
            = new ConcurrentDictionary<string, ICacheConnectionString>();

        // ReSharper disable once InconsistentNaming
        private static readonly ConcurrentDictionary<string, string> cacheNofifierAssociations
            = new ConcurrentDictionary<string, string>();

        static CacheManager()
        {
            InitializeFromConfig();
        }

        #region Cache access API

        /// <summary>Gets a cache instance using full name of calling method's class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCurrentClassCache()
        {
            var method = GetCallingMethod();
            var declaringType = method.DeclaringType;
            return GetCache(declaringType);
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache<TClass>()
        {
            return GetCache(typeof(TClass));
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(Type type)
        {
            return GetCache(type.FullName);
        }

        /// <summary>Gets a list of all cache names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetCacheNames()
        {
            string[] names;

            cachesSync.Wait();
            try
            {
                names = caches.Values.Select(v => v.Name).ToArray();
            }
            finally
            {
                cachesSync.Release();
            }

            return names;
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var cachesArray = GetCaches();
            
            var cachesSorted = cachesArray.OrderByDescending(c => c.Key.GetMatchLevel(name));
            var cache = cachesSorted.FirstOrDefault();

            return (cache.Key.Prefix != null && cache.Key.GetMatchLevel(name) >= cache.Key.Prefix.Length) ? cache.Value : null;
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static async Task<ICache> GetCacheAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var cachesArray = await GetCachesAsync().ConfigureAwait(false);
            
            var cachesSorted = cachesArray.OrderByDescending(c => c.Key.GetMatchLevel(name));
            var cache = cachesSorted.FirstOrDefault();

            return (cache.Key.Prefix != null && cache.Key.GetMatchLevel(name) >= cache.Key.Prefix.Length) ? cache.Value : null;
        }

        /// <summary>Gets a cache by name - return a specialized cache implementation type</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static TCache GetCache<TCache>(string name) where TCache : ICache
        {
            var cache = GetCache(name);
            if (!(cache is TCache))
                throw new ArgumentException("The specified cache is not of type " + typeof(TCache));

            return (TCache)cache;
        }

        /// <summary>Gets a list of all notifier names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetNotifierNames()
        {
            string[] names;

            notifiersSync.EnterReadLock();
            try
            {
                names = notifiers.Values.Select(v => v.Name).ToArray();
            }
            finally
            {
                notifiersSync.ExitReadLock();
            }

            return names;
        }

        /// <summary>Gets a notifier by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICacheNotifier GetNotifier(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            ICacheNotifier result;

            notifiersSync.EnterReadLock();
            try
            {
                notifiers.TryGetValue(name, out result);
            }
            finally
            {
                notifiersSync.ExitReadLock();
            }

            return result;
        }

        /// <summary>Gets a list of all connection string names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetConnectionStringNames()
        {
            string[] names;

            connectionStringsSync.EnterReadLock();
            try
            {
                names = connectionStrings.Values.Select(v => v.Name).ToArray();
            }
            finally
            {
                connectionStringsSync.ExitReadLock();
            }

            return names;
        }

        /// <summary>Gets a connection string by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICacheConnectionString GetConnectionString(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            ICacheConnectionString result;

            connectionStringsSync.EnterReadLock();
            try
            {
                connectionStrings.TryGetValue(name, out result);
            }
            finally
            {
                connectionStringsSync.ExitReadLock();
            }

            return result;
        }

        /// <summary>Associates a cache with a notifier</summary>
        /// <param name="cache"></param>
        /// <param name="notifier"></param>
        public static void Associate(ICache cache, ICacheNotifier notifier)
        {
            cacheNofifierAssociations.AddOrUpdate(
                cache.Name, c => notifier.Name, (c, n) => notifier.Name);
        }

        /// <summary>Gets the notifier that was associated with a cahe</summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static ICacheNotifier GetAssociatedNotifier(ICache cache)
        {
            return cacheNofifierAssociations.TryGetValue(cache.Name, out string notifierName)
                ? GetNotifier(notifierName)
                : null;
        }

        #endregion

        private static KeyValuePair<CacheName, ICache>[] GetCaches()
        {
            KeyValuePair<CacheName, ICache>[] cachesArray;

            cachesSync.Wait();
            try
            {
                cachesArray = caches.ToArray();
            }
            finally
            {
                cachesSync.Release();
            }

            return cachesArray;
        }

        private static async Task<KeyValuePair<CacheName, ICache>[]> GetCachesAsync()
        {
            KeyValuePair<CacheName, ICache>[] cachesArray;

            await cachesSync.WaitAsync().ConfigureAwait(false);
            try
            {
                cachesArray = caches.ToArray();
            }
            finally
            {
                cachesSync.Release();
            }

            return cachesArray;
        }

        private static void SyncSet(ReaderWriterLockSlim syncObj, Action action)
        {
            syncObj.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                syncObj.ExitWriteLock();
            }
        }

        private static void SyncSet(SemaphoreSlim syncObj, Action action)
        {
            syncObj.Wait();
            try
            {
                action();
            }
            finally
            {
                syncObj.Release();
            }
        }

        #region Cache configuration API

        /// <summary>
        /// Sets named cached according to application config (app.config/web.config)
        /// This method is called automatically by static constructor.
        /// Initialization cab be overriden using SetCache and RemoveCache (and this method)
        /// </summary>
        public static void InitializeFromConfig()
        {
            var config = LoadConfig();
            ApplyConfig(config);
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cache"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public static void SetCache(string name, ICache cache)
        {
            var cacheName = new CacheName(name);

            void Action()
            {
                if (cache == null)
                {
                    // ReSharper disable once UnusedVariable
                    caches.TryRemove(cacheName, out var oldValue);
                }
                else
                {
                    caches.AddOrUpdate(cacheName, cache, (k, v) => cache);
                }
            }

            SyncSet(cachesSync, Action);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveCache(string name)
        {
            var cacheName = new CacheName(name);

            void Action()
            {
                // ReSharper disable once UnusedVariable
                caches.TryRemove(cacheName, out var oldValue);
            }

            SyncSet(cachesSync, Action);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public static void RemoveAllCaches()
        {
            void Action()
            {
                caches.Clear();
            }

            SyncSet(cachesSync, Action);
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectionString"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public static void SetConnectionString(string name, ICacheConnectionString connectionString)
        {
            void Action()
            {
                if (connectionString == null)
                {
                    // ReSharper disable once UnusedVariable
                    connectionStrings.TryRemove(name, out var oldValue);
                }
                else
                {
                    connectionStrings.AddOrUpdate(name, connectionString, (k, v) => connectionString);
                }
            }

            SyncSet(connectionStringsSync, Action);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveConnectionString(string name)
        {
            void Action()
            {
                // ReSharper disable once UnusedVariable
                connectionStrings.TryRemove(name, out var oldValue);
            }

            SyncSet(connectionStringsSync, Action);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application intialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public static void RemoveAllConnectionStrings()
        {
            void Action()
            {
                connectionStrings.Clear();
            }

            SyncSet(connectionStringsSync, Action);
        }

        /// <summary>
        /// Adds or sets a notifier by name
        /// This method is to be used during application intialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="notifier"></param>
        public static void SetNotifier(string name, ICacheNotifier notifier)
        {
            void Action()
            {
                if (notifier == null)
                {
                    // ReSharper disable once UnusedVariable
                    notifiers.TryRemove(name, out var oldValue);
                }
                else
                {
                    notifiers.AddOrUpdate(name, notifier, (k, v) => notifier);
                }
            }

            SyncSet(notifiersSync, Action);
        }

        /// <summary>
        /// Removes a notifier registration with a given name.
        /// This method is to be used during application intialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveNotifier(string name)
        {
            void Action()
            {
                // ReSharper disable once UnusedVariable
                notifiers.TryRemove(name, out var oldValue);
            }

            SyncSet(notifiersSync, Action);
        }

        /// <summary>
        /// Removes all notifier registrations with any given name.
        /// This method is to be used during application intialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        public static void RemoveAllNotifiers()
        {
            void Action()
            {
                notifiers.Clear();
            }

            SyncSet(notifiersSync, Action);
        }

        #endregion

        #region Automatic cache naming

        // ReSharper disable once InconsistentNaming
        private static readonly object loadLock = new object();

        private static MethodBase GetCallingMethod()
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

        private static Func<MethodBase> CreateGetClassNameFunction()
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

            var stackFrame = Expression.New(constructor, Expression.Constant(3));
            var method = Expression.Call(stackFrame, getMethodMethod);
            var lambda = Expression.Lambda<Func<MethodBase>>(method);
            var compileFunction = lambda.GetType().GetMethod("Compile", new Type[0]);
            var function = (Func<MethodBase>)compileFunction.Invoke(lambda, null);

            return function;
        }

        #endregion

        #region Config Loading

        private static IList<ConfigNode> LoadConfig()
        {
            var config = ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<ConfigNode>;
            return config;
        }

        private static void ApplyConfig(IList<ConfigNode> config)
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
            foreach (var item in connectionConfigs)
                SetConnectionString(item.Name, item.CreateConnectionString());
            foreach (var item in notifierConfigs)
                SetNotifier(item.Name, item.CreateCacheNotifier());
            foreach (var item in cacheConfigs)
                SetCache(item.Name, item.CreateCache());
        }

        #endregion
    }
}
