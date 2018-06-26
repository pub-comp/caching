using System;
using System.Linq;
using System.Net;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching
{
    public class RedisClient : IDisposable
    {
        private readonly ConfigurationOptions config;
        private readonly NLog.ILogger log;
        private IConnectionMultiplexer innerContext;
        private IRedisMonitor redisMonitor;

        public RedisClient(String connectionString, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.config = ConfigurationOptions.Parse(connectionString);
            this.log = NLog.LogManager.GetLogger(nameof(RedisClient));

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
                log.Debug("Redis Reconnect: {0}", config.ToString(false));
                config.AbortOnConnectFail = false;
                this.innerContext = ConnectionMultiplexer.Connect(config);
            }
        }

        private void RedisMonitor(string clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            redisMonitor = RedisMonitorFactory.CreateMonitor(clusterType);
            if (redisMonitor == null)
                return;

            redisMonitor.StartMonitor(config, monitorPort, monitorIntervalMilliseconds, MasterChanged);
        }

        private bool MasterChanged(EndPoint endpoint)
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
                IServer master;
                if (redisMonitor != null)
                {
                    master = innerContext.GetServer(redisMonitor.MasterEndpoint);
                }
                else
                {
                    var servers = innerContext.GetEndPoints(false)
                        .Select(ep => innerContext.GetServer(ep)).ToList();

                    master = servers.FirstOrDefault(s => !s.IsSlave);
                }

                if (master == null)
                {
                    log.Fatal("GetMasterServer cannot detect master");
                    throw new ApplicationException("GetMasterServer cannot detect master");
                }

                log.Debug($"GetMasterServer {master.EndPoint}");
                return master;
            }
        }

        public void Dispose()
        {
            this.innerContext.Dispose();
        }
    }
}
