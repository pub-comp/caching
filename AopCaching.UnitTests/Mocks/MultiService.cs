using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubComp.Caching.Core.Attributes;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MultiService
    {
        private int getItemsNoCacheCounter;

        [CacheList("CacheMissing", typeof(MockDataKeyConverter))]
        public IList<MockData> GetItemsNoCache(IList<string> keys)
        {
            ++getItemsNoCacheCounter;
            return new List<MockData> { new MockData { Id = keys.FirstOrDefault(), Value = getItemsNoCacheCounter.ToString() } };
        }

        [CacheList("CacheMissing", typeof(MockDataKeyConverter))]
        public async Task<IList<MockData>> GetItemsNoCacheAsync(IList<string> keys)
        {
            ++getItemsNoCacheCounter;
            await Task.Delay(10);
            return new List<MockData> { new MockData { Id = keys.FirstOrDefault(), Value = getItemsNoCacheCounter.ToString() } };
        }

        [CacheList(typeof(MockDataKeyConverter))]
        public IList<MockData> GetItems(IList<string> keys)
        {
            return keys.Select(k => new MockData { Id = k }).ToList();
        }

        [CacheList(typeof(MockDataKeyConverter))]
        public IList<MockData> GetItems(IList<string> keys, [DoNotIncludeInCacheKey]object obj)
        {
            return keys.Select(k => new MockData { Id = k, Value = k + ((obj as MockObject)?.Data ?? 0).ToString() }).ToList();
        }

        [CacheList(typeof(MockDataKeyConverter))]
        public Task<IList<MockData>> GetItemsAsync(IList<string> keys)
        {
            return Task.Run<IList<MockData>>(() => keys.Select(k => new MockData {Id = k}).ToList());
        }

        [CacheList(typeof(MockDataKeyConverter))]
        public Task<IList<MockData>> GetItemsAsync(IList<string> keys, [DoNotIncludeInCacheKey]object obj)
        {
            return Task.Run<IList<MockData>>(() =>
                keys.Select(k => new MockData {Id = k, Value = k + ((obj as MockObject)?.Data ?? 0).ToString()})
                    .ToList());
        }
    }

    public class MockData
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class MockDataKeyConverter : IDataKeyConverter<string, MockData>
    {
        public string GetKey(MockData data)
        {
            return data.Id;
        }
    }
}
