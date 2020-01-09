using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public class CacheDirectives
    {
        public static string HeadersKey = $"X-{nameof(CacheDirectives)}";

        public CacheMethod Method { get; set; }
        public DateTimeOffset? MinimumValueTimestamp { get; set; }
    }
}
