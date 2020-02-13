using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public class NoCache : IScopedCache
    {
        private readonly string name;

        public bool IsActive { get; } = true;
        public object GetDetails() => null;

        public NoCache(string name)
        {
            this.name = name;
        }

        public NoCache() : this(string.Empty)
        {
        }

        public string Name
        {
            get { return name; }
        }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            value = default;
            return false;
        }

        public Task<TryGetResult<TValue>> TryGetAsync<TValue>(string key)
        {
            return Task.FromResult(new TryGetResult<TValue> {WasFound = false});
        }

        public void Set<TValue>(string key, TValue value)
        {
        }

        public Task SetAsync<TValue>(string key, TValue value)
        {
            return Task.FromResult<object>(null);
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            return getter();
        }

        public Task<TValue> GetAsync<TValue>(string key, Func<Task<TValue>> getter)
        {
            return getter();
        }

        public CacheMethodTaken SetScoped<TValue>(string key, TValue value, DateTimeOffset valueTimestamp)
        {
            return CacheMethodTaken.None;
        }

        public Task<CacheMethodTaken> SetScopedAsync<TValue>(string key, TValue value, DateTimeOffset valueTimestamp)
        {
            return Task.FromResult(CacheMethodTaken.None);
        }

        public GetScopedResult<TValue> GetScoped<TValue>(string key, Func<ScopedValue<TValue>> getter)
        {
            var scopedValue = getter();
            return new GetScopedResult<TValue>
            {
                MethodTaken = CacheMethodTaken.None, 
                ScopedValue = scopedValue
            };
        }

        public async Task<GetScopedResult<TValue>> GetScopedAsync<TValue>(string key, Func<Task<ScopedValue<TValue>>> getter)
        {
            var scopedValue = await getter().ConfigureAwait(false);
            return new GetScopedResult<TValue>
            {
                MethodTaken = CacheMethodTaken.None,
                ScopedValue = scopedValue
            };
        }

        public CacheMethodTaken TryGetScoped<TValue>(string key, out ScopedValue<TValue> scopedValue)
        {
            scopedValue = default;
            return CacheMethodTaken.None;
        }

        public Task<TryGetScopedResult<TValue>> TryGetScopedAsync<TValue>(string key)
        {
            return Task.FromResult(new TryGetScopedResult<TValue>
            {
                MethodTaken = CacheMethodTaken.None,
                ScopedValue = default
            });
        }

        public void Clear(String key)
        {
        }

        public Task ClearAsync(string key)
        {
            return Task.FromResult<object>(null);
        }

        public void ClearAll()
        {
        }

        public Task ClearAllAsync()
        {
            return Task.FromResult<object>(null);
        }
   }
}