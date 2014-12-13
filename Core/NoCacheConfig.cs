namespace PubComp.Caching.Core
{
    public class NoCacheConfig : CacheConfig
    {
        public override ICache CreateCache()
        {
            return new NoCache(this.Name);
        }
    }
}
