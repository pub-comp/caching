namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifierPolicy
    {
        /// <summary>
        /// Connection string to Redis. You must either fill this in or ConnectionName.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Connection string name. You must either fill this in or ConnectionString.
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Redic converter. Currently supports "json" (default), "bson", "deflate" or "gzip".
        /// </summary>
        public string Converter { get; set; }

        /// <summary>
        /// Redis ClusterType. Currently supports "replica" (default) or "none" (and in the future "cluster").
        /// </summary>
        public string ClusterType { get; set; }

        /// <summary>
        /// Redis sentinel monitoring port (default: 26379).
        /// </summary>
        public int MonitorPort { get; set; }

        /// <summary>
        /// The interval in milliseconds for monitoring the master-replica.
        /// </summary>
        public int MonitorIntervalMilliseconds { get; set; }

        /// <summary>
        /// Optional - subscribe to a general invalidation channel for cluster invalidation requests
        /// </summary>
        public string GeneralInvalidationChannel { get; set; }

        /// <summary>
        /// Optional - Automatic publish CacheItemActionTypes.Updated when overriding cache item with new value
        /// </summary>
        public bool InvalidateOnUpdate { get; set; }

        public RedisCacheNotifierPolicy()
        {
            ConnectionString = @"127.0.0.1:6379,serviceName=mymaster";
            MonitorIntervalMilliseconds = 5000;
            ClusterType = "none";
            Converter = "json";
            MonitorPort = 26379;
            InvalidateOnUpdate = false;
        }
    }
}
