using System;

namespace PubComp.Caching.Core.Exceptions
{
    [Serializable]
    public class CacheClearException : CacheException
    {
        public CacheClearException()
        {
        }

        public CacheClearException(string message) : base(message)
        {
        }

        public CacheClearException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
