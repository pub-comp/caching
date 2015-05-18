using System;

namespace PubComp.Caching.MongoDbCaching
{
    public class MongoDbCachePolicy
    {
        /// <summary>
        /// Required parameter - connection string to MongoDB
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Optional parameter - database name in MongoDB, defaults to CacheDb
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// If true, ExpireWithin will be relative to last access
        /// Only used if ExpireWithin is set
        /// Default is false
        /// </summary>
        public bool UseSlidingExpiration { get; set; }

        /// <summary>
        /// Time until expiration from insertion (if UseSlidingExpiration is false)
        /// or last access (if UseSlidingExpiration is true)
        /// Default is null
        /// </summary>
        public TimeSpan? ExpireWithin { get; set; }

        /// <summary>
        /// Expire At Date
        /// Default is null
        /// If this is set (as non-null) in conjunction to ExpireWithin,
        /// then ExpireWithin date is relative to ExpireAt instead of insertion date.
        /// If this is set (as non-null) in conjunction to both ExpireWithin and UseSlidingExpiration,
        /// then this value is overridden per-item on first read of item
        /// </summary>
        public DateTime? ExpireAt { get; set; }

        public MongoDbCachePolicy()
        {
            ConnectionString = new PubComp.NoSql.MongoDbDriver.MongoDbConnectionInfo().ConnectionString;
            DatabaseName = "CacheDb";
        }
    }
}
