using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubComp.Caching.Core.Attributes;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MultiService
    {
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
