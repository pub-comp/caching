using System;
using System.Collections.Generic;

namespace PubComp.Caching.Core.Config.Loaders
{
    public class CacheConfigLoadErrorsException : ApplicationException
    {
        public override string Message => string.Join(Environment.NewLine, errors);

        private readonly List<CacheConfigLoadError> errors;
        public CacheConfigLoadErrorsException()
        {
            errors = new List<CacheConfigLoadError>();
        }

        public void Add(CacheConfigLoadError cacheConfigLoadError)
        {
            errors.Add(cacheConfigLoadError);
        }

        public bool IsEmpty() => errors.Count == 0;

        public class CacheConfigLoadError
        {
            public string Error { get; set; }
            public Exception Exception { get; set; } = null;

            public override string ToString()
            {
                if (Exception != null)
                {
                    return $"Error:{Error} Exception:{Exception.Message}";
                }

                return $"Error:{Error}";
            }
        }
    }
}