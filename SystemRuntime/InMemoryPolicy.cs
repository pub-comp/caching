using System;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryPolicy : InMemoryExpirationPolicy
    {
        /// <summary>
        /// Notifications provider name. Default is null/undefined.
        /// </summary>
        public string SyncProvider { get; set; }

        /// <summary>
        /// If set, override expiration policy in case where the sync provider is down
        /// </summary>
        public InMemoryExpirationPolicy OnSyncProviderFailure { get; set; }

        /// <summary>
        /// Whether or not to lock on cache misses before calling underlying getter.
        /// </summary>
        /// <remarks>Default value is false, which indicates lock will be used.</remarks>
        public bool DoNotLock { get; set; }

        /// <summary>
        /// For a given key, a locking mechanism is used to prevent,
        /// on cache misses, concurrent calls to the same getter.
        /// This parameter enables defining how many different locks to use
        /// for enabling concurrent calls, for different keys in this specific cache.
        /// </summary>
        /// <remarks>Relevant only if DoNotLock was not set to true.</remarks>
        public ushort? NumberOfLocks { get; set; }

        /// <summary>
        /// If set, used as a timeout for concurrency lock.
        /// If not set, no timeout will be used.
        /// If set, an exception of type <see cref="PubComp.Caching.Core.Exceptions.CacheLockException"/> will be throw in case of timeout.
        /// </summary>
        public int? LockTimeoutMilliseconds { get; set; }

        /// <summary>
        /// Enables using <see cref="LockTimeoutMilliseconds"/> without exceptions.
        /// </summary>
        /// <returns>Defaults to true.</returns>
        public bool? DoThrowExceptionOnTimeout { get; set; }
    }
}
