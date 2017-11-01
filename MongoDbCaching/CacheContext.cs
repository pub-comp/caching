using System;

namespace PubComp.Caching.MongoDbCaching
{
    public class CacheContext : NoSql.MongoDbDriver.MongoDbContext, ICacheContext
    {
        private readonly String connectionString;

        public CacheContext(String connectionString, String dbName) : base(connectionString, dbName)
        {
            this.connectionString = connectionString;
        }

        public EntitySet<String, CacheItem> GetEntitySet(
            string cacheDbName, string cacheCollectionName, TimeSpan? timeToLive)
        {
            var set = (EntitySet<String, CacheItem>)this.GetEntitySet<String, CacheItem>(
                cacheDbName, cacheCollectionName);

            if (timeToLive.HasValue)
                CreateExpirationIndex(cacheDbName, cacheCollectionName, timeToLive.Value);

            return set;
        }

        private void CreateExpirationIndex(string dbName, string collectionName, TimeSpan timeToLive)
        {
            var mongoClient = new MongoDB.Driver.MongoClient(connectionString);
            var mongoDatabase = MongoDB.Driver.MongoClientExtensions.GetServer(mongoClient).GetDatabase(dbName);
            var collection = mongoDatabase.GetCollection<CacheItem>(collectionName);

            var keys = new MongoDB.Driver.Builders.IndexKeysBuilder();
            keys.Ascending("ExpireAt");

            var options = new MongoDB.Driver.Builders.IndexOptionsBuilder();
            options.SetName("ExpirationIndex");
            options.SetTimeToLive(timeToLive);
            options.SetSparse(false);
            options.SetUnique(false);
            options.SetBackground(true);

            collection.CreateIndex(keys, options);
        }
    }
}
