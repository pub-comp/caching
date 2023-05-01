using System;

namespace PubComp.Caching.Core
{
    public class CacheDirectives : IContext<CacheDirectives>
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

        public bool IsInScope<TValue>(ScopedValue<TValue> scopedValue)
        {
            return this.Method.HasFlag(CacheMethod.IgnoreMinimumValueTimestamp)
                   || scopedValue.ValueTimestamp >= this.MinimumValueTimestamp;
        }

        public bool IsValid()
        {
            if (this.Method.HasFlag(CacheMethod.Get) && !this.Method.HasFlag(CacheMethod.IgnoreMinimumValueTimestamp))
            {
                if (this.MinimumValueTimestamp == default || this.MinimumValueTimestamp > DateTimeOffset.UtcNow)
                {
                    return false;
                }
            }

            return true;
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

        public CacheDirectives Clone()
        {
            return new CacheDirectives(method: this.Method, minimumValueTimestamp: this.MinimumValueTimestamp);
        }

        public static CacheDirectives CurrentScope => ScopedContext<CacheDirectives>.CurrentContext;
        public static DateTimeOffset CurrentScopeTimestamp => ScopedContext<CacheDirectives>.CurrentTimestamp;
    }
}
