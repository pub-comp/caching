using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisClient : IDisposable
    {
        private ConfigurationOptions config;
        private IConnectionMultiplexer innerContext;
        private IRedisMonitor redisMonitor;

        public RedisClient(String connectionString, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            config = ConfigurationOptions.Parse(connectionString);
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

        public IDatabase Database
        {
            get { return this.innerContext.GetDatabase(); }
        }

        public ISubscriber Subscriber
        {
            get { return this.innerContext.GetSubscriber(); }
        }

        public IServer MasterServer
        {
            get
            {
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
            //return innerContext.GetServer("172.20.0.219:6379");
        }

        public void Dispose()
        {
            this.innerContext.Dispose();
        }
    }
}
