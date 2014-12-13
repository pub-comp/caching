using System;

namespace PubComp.Caching.Core
{
    public class NoCache : ICache
    {
        private readonly string name;

        public NoCache(string name)
        {
            this.name = name;
        }

        public NoCache() : this(string.Empty)
        {
        }

        public string Name { get { return name; } }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            return getter();
        }

        public void Clear(String key)
        {
        }

        public void ClearAll()
        {
        }
    }
}
