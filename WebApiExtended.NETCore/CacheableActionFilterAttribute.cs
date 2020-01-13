using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using PubComp.Caching.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PubComp.Caching.WebApiExtended.Net.Core
{
    public class CacheableActionFilterAttribute : TypeFilterAttribute, ICacheable
    {
        public CacheableActionFilterAttribute() : base(typeof(CacheableActionFilterImpl))
        {
        }

        private class CacheableActionFilterImpl : IAsyncActionFilter
        {
            private NLog.ILogger Logger => NLog.LogManager.GetLogger(typeof(CacheableActionFilterAttribute).FullName);

            public async Task OnActionExecutionAsync(
                ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                var definedCacheDirectives = GetCacheDirectives(context);
                using (ScopedContext<CacheDirectives>.CreateNewScope(definedCacheDirectives))
                {
                    await next().ConfigureAwait(false);
                }
            }

            private CacheDirectives GetCacheDirectives(ActionExecutingContext actionContext)
            {
                var cacheDirectivesJson = actionContext.HttpContext.Request.Headers[CacheDirectives.HeadersKey]
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(cacheDirectivesJson))
                    return new CacheDirectives {Method = CacheMethod.None};

                var cacheDirectives = JsonConvert.DeserializeObject<CacheDirectives>(cacheDirectivesJson);
                if (cacheDirectives.Method.HasFlag(CacheMethod.Get) &&
                    (cacheDirectives.MinimumValueTimestamp == DateTimeOffset.MinValue ||
                     cacheDirectives.MinimumValueTimestamp > DateTimeOffset.UtcNow))
                {
                    var newCacheMethod = cacheDirectives.Method ^ CacheMethod.Get;
                    Logger.Warn(
                        $"CacheMethod requested: {cacheDirectives.Method} has been demoted to {newCacheMethod} due to invalid {nameof(cacheDirectives.MinimumValueTimestamp)}: {cacheDirectives.MinimumValueTimestamp}");
                    cacheDirectives.Method = newCacheMethod;
                }

                return cacheDirectives;
            }
        }
    }
}