using System;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisCachePolicy
    {
        /// <summary>
        /// Required parameter - connection string to MongoDB
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Redic converter. Currently supports "json" (default) or "binary". 
        /// </summary>
        public string Converter { get; set; }

        /// <summary>
        /// Redis ClusterType. Currently supports "replica" (default) or "none" (in the future "cluster"). 
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
        /// Gets or sets a value that indicates whether a cache entry should be evicted at a specified time.
        /// </summary>
        /// <remarks>Default value is a date-time value that is set to the maximum possible value,
        /// which indicates that the entry does not expire at a pre-specified time</remarks>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a cache entry should be evicted
        /// within a given span of time since its addition.
        /// </summary>
        /// <remarks>Default value is a time-duration value that is set to zero,
        /// which indicates that a cache entry has no expiration from base on a time span from addition.</remarks>
        public TimeSpan? ExpirationFromAdd { get; set; }

        /// <summary>
        /// A span of time within which a cache entry must be accessed
        /// before the cache entry is evicted from the cache.
        /// </summary>
        /// <remarks>Default value is a time-duration value that is set to zero,
        /// which indicates that a cache entry has no sliding expiration time</remarks>
        public TimeSpan? SlidingExpiration { get; set; }

        public RedisCachePolicy()
        {
            ConnectionString = @"172.16.0.44:6379,172.16.0.48:6379,172.16.0.43:6379,serviceName=mymaster";
            MonitorIntervalMilliseconds = 5000;
            ClusterType = "replica";
            Converter = "json";
            MonitorPort = 26379;
        }
    }
}
