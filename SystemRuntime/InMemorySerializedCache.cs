using System;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemorySerializedCache : ObjectCache
    {
        public InMemorySerializedCache(String name, InMemoryPolicy policy)
            : base(name, new System.Runtime.Caching.MemoryCache(name), policy)
        {
        }

        public InMemorySerializedCache(String name, TimeSpan slidingExpiration)
            : this(name,
                new InMemoryPolicy
                {
                    SlidingExpiration = slidingExpiration
                })
        {
        }

        public InMemorySerializedCache(String name, DateTimeOffset absoluteExpiration)
            : this(name,
                new InMemoryPolicy
                {
                    AbsoluteExpiration = absoluteExpiration
                })
        {
        }

        protected override bool TryGetInner<TValue>(string key, out TValue value)
        {
            object val = InnerCache.Get(key, null);

            if (val != null)
            {
                value = Newtonsoft.Json.JsonConvert.DeserializeObject<TValue>(val.ToString());
                return true;
            }

            value = default(TValue);
            return false;
        }

        protected override void Add<TValue>(string key, TValue value)
        {
            var val = Newtonsoft.Json.JsonConvert.SerializeObject(
                value,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                });

            InnerCache.Add(key, val, GetRuntimePolicy(), null);
        }
    }
}
