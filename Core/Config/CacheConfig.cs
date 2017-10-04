namespace PubComp.Caching.Core.Config
{
    public abstract class CacheConfig : ConfigNode
    {
        public abstract ICache CreateCache();
    }
}
