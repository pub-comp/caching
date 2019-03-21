using System;

namespace PubComp.Caching.Core.Exceptions
{
    [Serializable]
    public class CacheLockException : CacheException
    {
        public CacheLockException()
        {
        }

        public CacheLockException(string message) : base(message)
        {
        }

        public CacheLockException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
