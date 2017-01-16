using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching.StackExchange.UnitTests.Mocks
{
    class MockCacheItem
    {
        public static MockCacheItem GetNewMockInstance(string key)
        {
            return new MockCacheItem(key);
        }

        private MockCacheItem(string key)
        {
            Key = "key_" + key;
            Value = "value_" + key;
            Payload = "payload_" + key + "_" + Guid.NewGuid().ToString();
            TimeUtc = DateTime.UtcNow;
        }

        public string Key { get; set; }
        public string Value { get; set; }
        public string Payload { get; set; }
        public DateTime TimeUtc { get; set; }

    }
}
