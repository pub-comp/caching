using System;

namespace PubComp.Caching.Core
{
    public class CacheDirectives
    {
        public static string HeadersKey = $"X-{nameof(CacheDirectives)}";

        public CacheMethod Method { get; set; }
        public DateTimeOffset MinimumValueTimestamp { get; set; }

        public CacheDirectives()
        {
        }

        public CacheDirectives(CacheMethod method, DateTimeOffset minimumValueTimestamp)
        {
            this.Method = method;
            this.MinimumValueTimestamp = minimumValueTimestamp;
        }

        public static IDisposable SetScope(CacheMethod method, DateTimeOffset minimumValueTimestamp)
        {
            return ScopedContext<CacheDirectives>.CreateNewScope(new CacheDirectives(method, minimumValueTimestamp));
        }
    }
}
