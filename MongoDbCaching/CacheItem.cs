using System;
using PubComp.NoSql.Core;
using PubComp.NoSql.MongoDbDriver;

namespace PubComp.Caching.MongoDbCaching
{
    public class CacheItem : IEntity<String>
    {
        public String Id { get; set; }
        public Object Value { get; set; }
        public DateTime? ExpireAt { get; set; }

        public CacheItem()
        {
        }

        public CacheItem(String id, Object value)
        {
            this.Id = id;
            this.Value = value;
        }

        public CacheItem(String id, Object value, DateTime? ExpireAt)
        {
            this.Id = id;
            this.Value = value;
            this.ExpireAt = ExpireAt;
        }
    }
}
