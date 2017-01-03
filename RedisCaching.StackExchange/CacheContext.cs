using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class CacheContext : IDisposable
    {
        private ConfigurationOptions config;
        private IRedisConverter convert;
        private IConnectionMultiplexer innerContext;
        private IRedisMonitor redisMonitor;

        public CacheContext(String connectionString, String converterType, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            config = ConfigurationOptions.Parse(connectionString);
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            RedisConnect();
            RedisMonitor(clusterType, monitorPort, monitorIntervalMilliseconds);
        }

        private void RedisConnect()
        {
            try
            {
                if (this.innerContext != null)
                {
                    this.innerContext.Close();
                    this.innerContext.Dispose();
                    this.innerContext = null;
                }
            }
            finally
            {
                LogHelper.Log.Debug("Redis Reconnect: {0}", config);
                this.innerContext = ConnectionMultiplexer.Connect(config);
            }
        }

        private void RedisMonitor(string clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            redisMonitor = RedisMonitorFactory.CreateMonitor(clusterType);
            if (redisMonitor == null)
            {
                return;
            }
            redisMonitor.StartMonitor(config, monitorPort, monitorIntervalMilliseconds, MasterChanged);
        }

        private bool MasterChanged(IPEndPoint endpoint)
        {
            RedisConnect();
            return true;
        }

        private IDatabase Database
        {
            get { return this.innerContext.GetDatabase(); }
        }

        private IServer GetMasterServer()
        {
            //return innerContext.GetServer("172.20.0.219:6379");
            IServer master = null;
            if (redisMonitor != null)
            {
                string serverEndpoint = string.Format("{0}:6379", redisMonitor.MasterEndpoint.Address);
                master = innerContext.GetServer(serverEndpoint);
            }
            else
            {
                var servers = innerContext.GetEndPoints(false)
                .Select(ep => innerContext.GetServer(ep)).ToList();
                master = servers.Where(s => !s.IsSlave).FirstOrDefault();
                
            }

            if (master == null)
            {
                LogHelper.Log.Fatal("GetMasterServer cannot detect master");
                throw new Exception("GetMasterServer cannot detect master");
            }

            LogHelper.Log.Debug("GetMasterServer {0}", ((IPEndPoint)master.EndPoint).Address);
            return master;
        }

        internal CacheItem<TValue> GetItem<TValue>(String cacheName, String key)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            var cacheItemString = Database.StringGet(id);
            return convert.FromRedis<TValue>(cacheItemString);
        }

        internal void SetItem<TValue>(CacheItem<TValue> cacheItem)
        {
            TimeSpan? expiry = null;
            if (cacheItem.ExpireIn.HasValue)
                expiry = cacheItem.ExpireIn;

            Database.StringSet(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always, CommandFlags.FireAndForget);
        }

        internal bool SetIfNotExists<TValue>(CacheItem<TValue> cacheItem)
        {
            if (Contains(cacheItem.Id))
            {
                return false;
            }
            SetItem(cacheItem);
            return true;
        }

        private bool Contains(string key)
        {
            return Database.KeyExists(key);
        }

        internal void SetExpirationTime<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem.ExpireIn.HasValue)
                ExpireById(cacheItem.Id, cacheItem.ExpireIn.Value);
        }

        internal void ExpireItemIn<TValue>(String cacheName, String key, TimeSpan timeSpan)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            ExpireById(id, timeSpan);
        }

        private void ExpireById(string id, TimeSpan timeSpan)
        {
            Database.KeyExpire(id, timeSpan, CommandFlags.FireAndForget);
        }

        internal void RemoveItem(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            Database.KeyDelete(id, CommandFlags.FireAndForget);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = GetMasterServer().Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            Database.KeyDelete(keys, CommandFlags.FireAndForget);
        }

        public void Dispose()
        {
            this.innerContext.Dispose();
        }
    }
}
