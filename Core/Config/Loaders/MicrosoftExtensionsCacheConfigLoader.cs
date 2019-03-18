using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;

namespace PubComp.Caching.Core.Config.Loaders
{
    // TODO: Add XML comments
    public class MicrosoftExtensionsCacheConfigLoader : ICacheConfigLoader
    {
        private readonly IConfigurationSection pubCompCacheConfigurationSection;

        public MicrosoftExtensionsCacheConfigLoader(IConfigurationSection pubCompCacheConfigurationSection)
        {
            this.pubCompCacheConfigurationSection = pubCompCacheConfigurationSection;
        }

        public MicrosoftExtensionsCacheConfigLoader(IConfiguration pubCompCacheConfiguration)
        : this(pubCompCacheConfiguration.GetSection("PubComp:CacheConfig"))
        {
        }
        public IList<ConfigNode> LoadConfig()
        {
            var configNodes = new List<ConfigNode>();
            var cacheConfigs =
                pubCompCacheConfigurationSection
                    .GetChildren();

            foreach (var cacheConfig in cacheConfigs)
            {
                if (cacheConfig.GetValue<ConfigAction>("action") == ConfigAction.Remove)
                {
                    configNodes.Add(new RemoveConfig { Action = ConfigAction.Remove, Name = cacheConfig["name"] });
                    continue;
                }

                var typeName = cacheConfig["type"] + "Config";
                var assemblyName = cacheConfig["assembly"];
                Assembly assembly;

                try
                {
                    // TODO: Reusage of Assemblies
                    assembly = Assembly.Load(assemblyName);
                }
                catch (System.IO.FileLoadException ex)
                {
                    LogConfigError($"Could not load assembly {assemblyName}", ex);
                    continue;
                }
                catch (BadImageFormatException ex)
                {
                    LogConfigError($"Could not load assembly {assemblyName}", ex);
                    continue;
                }

                var configType = assembly.GetType(typeName, false, false);
                if (configType == null)
                {
                    configType = assembly.GetType(assemblyName + '.' + typeName, false, false);
                }

                if (configType == null)
                {
                    LogConfigError($"Could not load type {typeName} from assembly {assemblyName}");
                    continue;
                }

                configNodes.Add(cacheConfig.Get(configType) as ConfigNode);
            }

            return configNodes;
        }

        private void LogConfigError(string error, Exception ex = null)
        {
            if (ex != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}. Exception: {ex}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}");
            }
        }
    }
}