using System.Collections.Generic;

namespace PubComp.Caching.Core.Config.Loaders
{
    // TODO: Add XML comments
    public interface ICacheConfigLoader
    {
        IList<ConfigNode> LoadConfig();
    }
}
