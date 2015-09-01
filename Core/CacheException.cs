using System;

namespace PubComp.Caching.Core
{
    [Serializable]
    public class CacheException : ApplicationException
    {
        public CacheException()
        {
        }

        public CacheException(string message) : base(message)
        {
        }

        public CacheException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
