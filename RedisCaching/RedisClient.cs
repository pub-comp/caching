using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace PubComp.Caching.RedisCaching
{
    public class RedisClient : IDisposable
    {
        private readonly ConfigurationOptions config;
        private readonly NLog.ILogger log;
        private IConnectionMultiplexer innerContext;
        private IRedisMonitor redisMonitor;
        public event EventHandler<Core.Events.ProviderStateChangedEventArgs> OnRedisConnectionStateChanged;

        public static ConcurrentDictionary<string, Lazy<RedisClient>> ActiveRedisClients = new ConcurrentDictionary<string, Lazy<RedisClient>>();

        public static RedisClient GetNamedRedisClient(string connectionName) => GetNamedRedisClient(connectionName, null);
        public static RedisClient GetNamedRedisClient(string connectionName, EventHandler<Core.Events.ProviderStateChangedEventArgs> providerStateChangedCallback)
        {
            var redisConnectionConfig = PubComp.Caching.Core.CacheManager.GetConnectionString(connectionName);

            var lazyRedisClientCreator = new Lazy<RedisClient>(() =>
            {
                var policy = (redisConnectionConfig as RedisConnectionString)?.Policy ?? new RedisClientPolicy();
                return new RedisClient(redisConnectionConfig.ConnectionString, policy.ClusterType, policy.MonitorPort,
                    policy.MonitorIntervalMilliseconds);
            }, isThreadSafe: true);

            var client = ActiveRedisClients.GetOrAdd(connectionName, lazyRedisClientCreator).Value;

            if (providerStateChangedCallback != null)
            {
                client.OnRedisConnectionStateChanged += providerStateChangedCallback;
                if (!client.IsConnected)
                    providerStateChangedCallback(client, new Core.Events.ProviderStateChangedEventArgs(client.IsConnected));
            }

            return client;
        }

        public RedisClient(String connectionString, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.config = ConfigurationOptions.Parse(connectionString);
            this.log = NLog.LogManager.GetLogger(nameof(RedisClient));

            RedisConnect();
            RedisMonitor(clusterType, monitorPort, monitorIntervalMilliseconds);
        }

        public bool IsConnected => this.innerContext?.IsConnected ?? false;

        private bool? lastConnectionStateChangedValue = null;
        private void InvokeConnectionStateChangedCallback(bool newState)
        {
            if ((lastConnectionStateChangedValue ?? !newState) == newState)
                return;

            lastConnectionStateChangedValue = newState;

            try
            {
                OnRedisConnectionStateChanged(this, new Core.Events.ProviderStateChangedEventArgs(newState));
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to invoke ConnectionStateChanged");
            }
        }

        private void OnConnectionMultiplexerConnectivityEvent(object sender, EventArgs args)
        {
            InvokeConnectionStateChangedCallback(IsConnected);
        }

        public void RegisterConnectionEvents(IConnectionMultiplexer connectionMultiplexer)
        {
            connectionMultiplexer.ConnectionFailed += OnConnectionMultiplexerConnectivityEvent;
            connectionMultiplexer.ConnectionRestored += OnConnectionMultiplexerConnectivityEvent;
        }

        public void DeregisterConnectionEvents(IConnectionMultiplexer connectionMultiplexer)
        {
            connectionMultiplexer.ConnectionFailed -= OnConnectionMultiplexerConnectivityEvent;
            connectionMultiplexer.ConnectionRestored -= OnConnectionMultiplexerConnectivityEvent;
        }

        private void RedisConnect()
        {
            try
            {
                if (this.innerContext != null)
                {
                    DeregisterConnectionEvents(this.innerContext);
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
                RegisterConnectionEvents(this.innerContext);
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
