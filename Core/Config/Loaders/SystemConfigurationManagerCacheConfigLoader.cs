using System.Collections.Generic;
using System.Configuration;

namespace PubComp.Caching.Core.Config.Loaders
{
    /// <summary>
    /// Load the cache configuration using App/Web.config files using <see cref="System.Configuration.ConfigurationManager" />
    /// </summary>
    public class SystemConfigurationManagerCacheConfigLoader : ICacheConfigLoader
    {
        public IList<ConfigNode> LoadConfig()
        {
            return ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<ConfigNode>;
        }
    }
}