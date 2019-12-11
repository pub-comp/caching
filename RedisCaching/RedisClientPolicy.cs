namespace PubComp.Caching.RedisCaching
{
    public class RedisClientPolicy
    {
        /// <summary>
        /// Connection string to Redis. You must either fill this in or ConnectionName.
        /// </summary>
        public string ConnectionString { get; set; }

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

        public RedisClientPolicy()
        {
            ConnectionString = @"127.0.0.1:6379,serviceName=mymaster";
            MonitorIntervalMilliseconds = 5000;
            ClusterType = "none";
            MonitorPort = 26379;
        }
    }
}
