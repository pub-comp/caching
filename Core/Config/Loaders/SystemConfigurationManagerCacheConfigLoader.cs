using System.Collections.Generic;
using System.Configuration;

namespace PubComp.Caching.Core.Config.Loaders
{
    // TODO: Add XML comments
    public class SystemConfigurationManagerCacheConfigLoader : ICacheConfigLoader
    {
        public IList<ConfigNode> LoadConfig()
        {
            return ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<ConfigNode>;
        }
    }
}