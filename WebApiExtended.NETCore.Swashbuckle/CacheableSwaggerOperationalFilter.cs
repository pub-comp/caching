using Newtonsoft.Json;
using PubComp.Caching.Core;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PubComp.Caching.WebApiExtended.Net.Core
{
    public class CacheableSwaggerOperationalFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var cacheableAttribute = context.MethodInfo.GetCustomAttributes(true)
                .OfType<ICacheable>()
                .SingleOrDefault();

            if (cacheableAttribute == null)
            {
                return;
            }

            if (operation.Parameters == null)
                operation.Parameters = new List<IParameter>();
            var parameters = operation.Parameters;

            var example = JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { nameof(CacheDirectives.Method) , cacheableAttribute.DefaultMethod.ToString() },
                    { nameof(CacheDirectives.MinimumValueTimestamp), DateTimeOffset.UtcNow.AddMilliseconds(-Math.Abs(cacheableAttribute.DefaultMinimumAgeInMilliseconds)) }
                });

            var cacheDirectivesParameter = new NonBodyParameter
            {
                In = "header",
                Name = CacheDirectives.HeadersKey,
                Description = $"Method (enum: {string.Join(", ", Enum.GetNames(typeof(CacheMethod)))}), MinimumValueTimestamp (DateTimeOffset?)",
                Default = example,
                Type = "object"
            };
            parameters.Add(cacheDirectivesParameter);
        }
    }
}