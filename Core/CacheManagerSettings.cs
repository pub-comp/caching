using PubComp.Caching.Core.Config.Loaders;

namespace PubComp.Caching.Core
{
    /// <summary>
    /// Initialization time settings for the Cache Manager
    /// </summary>
    public class CacheManagerSettings
    {
        /// <summary>
        /// The source from which the entire cache configuration will be loaded from.
        /// The default value is to use System.ConfigurationManager internally to load from app/web.config
        /// This setting should be set before any call to the API methods
        /// </summary>
        public ICacheConfigLoader ConfigLoader { get; set; }

        /// <summary>
        /// If set to true, after CacheManager initialization done
        /// all the cache names will be automatically registered with CacheControllerUtil.
        /// </summary>
        public bool ShouldRegisterAllCaches { get; set; }
    }
}
