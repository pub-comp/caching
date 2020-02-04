using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using PubComp.Caching.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                var requestedCacheDirectives = GetCacheDirectivesFromRequest(context);
                using (CacheDirectives.SetScope(requestedCacheDirectives))
                {
                    await next().ConfigureAwait(false);
                }
            }

            private CacheDirectives GetCacheDirectivesFromRequest(ActionExecutingContext actionContext)
            {
                try
                {
                    var cacheDirectivesJson = actionContext.HttpContext.Request.Headers[CacheDirectives.HeadersKey]
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(cacheDirectivesJson))
                        return new CacheDirectives {Method = CacheMethod.None};

                    var cacheDirectives = JsonConvert.DeserializeObject<CacheDirectives>(cacheDirectivesJson);
                    if (cacheDirectives.IsValid())
                        return cacheDirectives;

                    Logger.Warn($"CacheMethod requested: {cacheDirectives.Method} has been demoted to CacheMethod.{nameof(CacheMethod.None)} due to invalid request: {cacheDirectivesJson}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to retrieve/parse CacheDirectives, CacheMethod.None");
                }
                return new CacheDirectives { Method = CacheMethod.None };
            }
        }
    }
}