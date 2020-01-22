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

        public static IDisposable SetScope(CacheDirectives cacheDirectives)
        {
            var scopeContext = new CacheDirectives(cacheDirectives.Method, cacheDirectives.MinimumValueTimestamp);
            return ScopedContext<CacheDirectives>.CreateNewScope(scopeContext);
        }

        public static IDisposable SetScope(CacheMethod method, DateTimeOffset minimumValueTimestamp)
        {
            return ScopedContext<CacheDirectives>.CreateNewScope(new CacheDirectives(method, minimumValueTimestamp));
        }

        public static CacheDirectives CurrentScope => ScopedContext<CacheDirectives>.CurrentContext;
        public static DateTimeOffset CurrentScopeTimestamp => ScopedContext<CacheDirectives>.CurrentTimestamp;
    }
}
