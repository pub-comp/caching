using System;
using System.Runtime.Serialization;

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

        protected CacheClearException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
