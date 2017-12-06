using System;

namespace PubComp.Caching.Core
{
    public interface ICache
    {
        string Name { get; }

        bool TryGet<TValue>(String key, out TValue value);

        void Set<TValue>(String key, TValue value);

        TValue Get<TValue>(String key, Func<TValue> getter);

        void Clear(String key);

        void ClearAll();
    }
}
