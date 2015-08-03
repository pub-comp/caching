namespace PubComp.Caching.Core
{
    public class LayeredCachePolicy
    {
        /// <summary>
        /// Required parameter - level1 cache name
        /// </summary>
        public string Level1CacheName { get; set; }

        /// <summary>
        /// Required parameter - level2 cache name
        /// </summary>
        public string Level2CacheName { get; set; }
    }
}
