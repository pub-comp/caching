using System;
using System.Collections.Generic;
using System.Linq;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MultiService2
    {
        [CacheList(typeof(MockData2KeyConverter))]
        public IList<MockData2> GetItems(IList<Guid> keys)
        {
            return keys.Select(k => new MockData2 { Id = k }).ToList();
        }

        [CacheList(typeof(MockData2KeyConverter))]
        public IList<MockData2> GetItems(IList<Guid> keys, [DoNotIncludeInCacheKey]object obj)
        {
            return keys.Select(k => new MockData2 { Id = k, Value = k + (obj != null ? obj.GetHashCode() : 0).ToString() }).ToList();
        }
    }

    public class MockData2
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    public class MockData2KeyConverter : IDataKeyConverter<Guid, MockData2>
    {
        public Guid GetKey(MockData2 data)
        {
            return data.Id;
        }
    }
}
