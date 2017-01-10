using System;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisCacheNotificationsPolicy
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
        
        public RedisCacheNotificationsPolicy()
        {
            ConnectionString = @"172.16.0.44:6379,172.16.0.48:6379,172.16.0.43:6379,serviceName=mymaster";
            MonitorIntervalMilliseconds = 5000;
            ClusterType = "replica";
            Converter = "json";
            MonitorPort = 26379;
        }
    }
}
