namespace PubComp.Caching.Core
{
    public abstract class CacheConfig
    {
        public ConfigAction Action { get; set; }

        public string Name { get; set; }

        public abstract ICache CreateCache();
    }
}
