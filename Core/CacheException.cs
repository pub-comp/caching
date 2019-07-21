using System;
using System.Runtime.Serialization;

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

        protected CacheException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
