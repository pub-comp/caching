using System.Collections.Generic;

namespace PubComp.Caching.Core.Config.Loaders
{
    /// <summary>
    /// The interface provides a load method and should be used to load the cache configuration from different sources 
    /// </summary>
    public interface ICacheConfigLoader
    {
        /// <summary>
        /// Load the configuration data from the source and return it as an ordered list of relevant ConfigNode types
        /// </summary>
        /// <returns>The list of Cache ConfigNode (and its inheriting classes)</returns>
        IList<ConfigNode> LoadConfig();
    }
}
