using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PubComp.Caching.Core.Config.Loaders;
using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.Core
{
    // TODO: Add XML comments
    // TODO: Create interfaces
    public class CacheManager
    {
        #region innerInstance

        private static CacheManagerLogic _innerCacheManagerInstance;
        private static readonly object _instanceGeneratorLock = new object();

        // The reason the logic init is split to CTOR and InitializeFromConfig is
        // to allow the calls to CacheManager while loading and applying the configuration
        // TODO: Change to internal to be accessed by unit tests
        // TODO: Pass ICacheManager or subset to ConfigNode classes that call CacheManager directly
        public static CacheManagerLogic CacheManagerLogic
        {
            get
            {
                if (_innerCacheManagerInstance == null)
                {
                    lock (_instanceGeneratorLock)
                    {
                        if (_innerCacheManagerInstance == null)
                        {
                            _innerCacheManagerInstance = new CacheManagerLogic(ConfigLoader);
                            _innerCacheManagerInstance.InitializeFromConfig();
                        }
                    }
                }

                return _innerCacheManagerInstance;
            }
            set
            {
                _innerCacheManagerInstance = value;
            }
        }

        #endregion

        public static ICacheConfigLoader ConfigLoader { get; set; } = new SystemConfigurationManagerCacheConfigLoader();

        #region Cache access API

        /// <summary>Gets a cache instance using full name of calling method's class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCurrentClassCache()
        {
            return CacheManagerLogic.GetCurrentClassCache();
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache<TClass>()
        {
            return CacheManagerLogic.GetCache<TClass>();
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(Type type)
        {
            return CacheManagerLogic.GetCache(type);
        }

        /// <summary>Gets a list of all cache names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetCacheNames()
        {
            return CacheManagerLogic.GetCacheNames();
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(string name)
        {
            return CacheManagerLogic.GetCache(name);
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static async Task<ICache> GetCacheAsync(string name)
        {
            return await CacheManagerLogic.GetCacheAsync(name).ConfigureAwait(false);
        }

        /// <summary>Gets a cache by name - return a specialized cache implementation type</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static TCache GetCache<TCache>(string name) where TCache : ICache
        {
            return CacheManagerLogic.GetCache<TCache>(name);
        }

        /// <summary>Gets a list of all notifier names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetNotifierNames()
        {
            return CacheManagerLogic.GetNotifierNames();
        }

        /// <summary>Gets a notifier by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICacheNotifier GetNotifier(string name)
        {
            return CacheManagerLogic.GetNotifier(name);
        }

        /// <summary>Gets a list of all connection string names</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static IEnumerable<string> GetConnectionStringNames()
        {
            return CacheManagerLogic.GetConnectionStringNames();
        }

        /// <summary>Gets a connection string by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICacheConnectionString GetConnectionString(string name)
        {
            return CacheManagerLogic.GetConnectionString(name);
        }

        /// <summary>Associates a cache with a notifier</summary>
        /// <param name="cache"></param>
        /// <param name="notifier"></param>
        public static void Associate(ICache cache, ICacheNotifier notifier)
        {
            CacheManagerLogic.Associate(cache, notifier);
        }

        /// <summary>Gets the notifier that was associated with a cahe</summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static ICacheNotifier GetAssociatedNotifier(ICache cache)
        {
            return CacheManagerLogic.GetAssociatedNotifier(cache);
        }

        #endregion

        #region Cache configuration API

        /// <summary>
        /// Sets named cached according to set config loader
        /// This method is called automatically once by inner instance getter.
        /// Initialization cab be overriden using SetCache and RemoveCache (and this method)
        /// </summary>
        public static void InitializeFromConfig()
        {
            CacheManagerLogic.InitializeFromConfig();
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cache"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public static void SetCache(string name, ICache cache)
        {
            CacheManagerLogic.SetCache(name, cache);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveCache(string name)
        {
            CacheManagerLogic.RemoveCache(name);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public static void RemoveAllCaches()
        {
            CacheManagerLogic.RemoveAllCaches();
        }

        /// <summary>
        /// Adds or sets a cache by name
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectionString"></param>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public static void SetConnectionString(string name, ICacheConnectionString connectionString)
        {
            CacheManagerLogic.SetConnectionString(name, connectionString);
        }

        /// <summary>
        /// Removes a cache registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveConnectionString(string name)
        {
            CacheManagerLogic.RemoveConnectionString(name);
        }

        /// <summary>
        /// Removes all cache registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the cache if already in use!
        /// </summary>
        public static void RemoveAllConnectionStrings()
        {
            CacheManagerLogic.RemoveAllConnectionStrings();
        }

        /// <summary>
        /// Adds or sets a notifier by name
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="notifier"></param>
        public static void SetNotifier(string name, ICacheNotifier notifier)
        {
            CacheManagerLogic.SetNotifier(name, notifier);
        }

        /// <summary>
        /// Removes a notifier registration with a given name.
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveNotifier(string name)
        {
            CacheManagerLogic.RemoveNotifier(name);
        }

        /// <summary>
        /// Removes all notifier registrations with any given name.
        /// This method is to be used during application initialization, it does not delete or replace the notifier if already in use!
        /// </summary>
        public static void RemoveAllNotifiers()
        {
            CacheManagerLogic.RemoveAllNotifiers();
        }

        #endregion
    }
}
