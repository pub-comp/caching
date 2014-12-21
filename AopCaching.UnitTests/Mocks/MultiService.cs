using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MultiService
    {
        [CacheList(typeof(MockDataKeyConverter))]
        public IList<MockData> GetItems(IList<string> keys)
        {
            return keys.Select(k => new MockData { Id = k }).ToList();
        }
    }

    public class MockData
    {
        public string Id { get; set; }
    }

    public class MockDataKeyConverter : IDataKeyConverter<string, MockData>
    {
        public string GetKey(MockData data)
        {
            return data.Id;
        }
    }
}
