using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryPolicy
    {
        public InMemoryPolicy()
        {
            AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
            ExpirationFromAdd = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
        }

        
        /// <summary>
        /// Gets or sets a value that indicates whether a cache entry should be evicted
        /// after a specified time.
        /// </summary>
        /// <remarks>Default value is a date-time value that is set to the maximum possible value,
        /// which indicates that the entry does not expire at a pre-specified time</remarks>
        public DateTimeOffset AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a cache entry should be evicted
        /// within a given span of time since its addition.
        /// </summary>
        /// <remarks>Default value is a time-duration value that is set to zero,
        /// which indicates that a cache entry has no expiration from base on a time span from addition.</remarks>
        public TimeSpan ExpirationFromAdd { get; set; }

        /// <summary>
        /// A span of time within which a cache entry must be accessed
        /// before the cache entry is evicted from the cache.
        /// </summary>
        /// <remarks>Default value is a time-duration value that is set to zero,
        /// which indicates that a cache entry has no sliding expiration time</remarks>
        public TimeSpan SlidingExpiration { get; set; }
    }
}
