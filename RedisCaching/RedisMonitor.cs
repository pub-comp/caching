using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching
{
    internal class RedisMonitorFactory
    {
        public static IRedisMonitor CreateMonitor(string type)
        {
            if (type == null || type == "none")
            {
                return null;
            }
            if (type == "replica")
            {
                return new RedisReplicaMonitor();
            }
            return null;
        }
    }
    
    internal interface IRedisMonitor
    {
        void StartMonitor(
            ConfigurationOptions config,
            int monitorPort,
            int monitorIntervalMilliseconds,
            Func<EndPoint, bool> masterChanged);

        void StopMonitor();

       EndPoint MasterEndpoint { get; }
    }

    internal class RedisReplicaMonitor : IRedisMonitor
    {
        private IConnectionMultiplexer innerConnection;
        private EndPoint master;
        private Func<EndPoint, bool> onMasterChanged;
        private int port = 26379;
        private string serviceName = "mymaster";
        private readonly NLog.ILogger log;

        internal RedisReplicaMonitor()
        {
            this.log = NLog.LogManager.GetLogger(nameof(RedisReplicaMonitor));
        }

        private bool Monitoring { get; set; }

        public EndPoint MasterEndpoint
        {
            get
            {
                return master;
            }
        }

        public void StartMonitor(ConfigurationOptions config, int monitorPort, int monitorIntervalMilliseconds,
            Func<EndPoint, bool> masterChanged)
        {
            try
            {
                port = monitorPort;
                serviceName = config.ServiceName;
                innerConnection = GetConnection(config);
                this.onMasterChanged = masterChanged;
                Monitoring = true;
                MonitorOnce();
                MonitorPush();
            }
            catch (Exception exp)
            {
                log.Fatal("No master found. Reason:{0}", exp.Message);
                throw new EntryPointNotFoundException("No master found");
            }
        }

        public void StopMonitor()
        {
            Monitoring = false;
        }

        private IEnumerable<IServer> GetServers()
        {
            var servers = innerConnection.GetEndPoints(false)
                .Select(ep => innerConnection.GetServer(ep)).ToList();
            return servers;
        }

        private ConnectionMultiplexer GetConnection(ConfigurationOptions config)
        {
            // Create a connection
            var options = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                AllowAdmin = true,
                TieBreaker = "",
                ServiceName = serviceName,
                SyncTimeout = 5000
            };

            IEnumerable<EndPoint> sentinels = config.EndPoints.ToList();
            foreach (var address in sentinels)
            {
                options.EndPoints.Add(address);
            }

            options.AbortOnConnectFail = false;
            var connection = ConnectionMultiplexer.Connect(options, Console.Out);
            return connection;
        }

        private void MonitorPush()
        {
            innerConnection.GetSubscriber(serviceName).SubscribeAsync("+switch-master", (channel, message) =>
            {
                Debug.WriteLine("Master Changed Event from Sentinel: {0}:{1}", channel, message);
                Monitor();
            }).Wait();
        }

        private void MonitorPull(int monitorIntervalMilliseconds)
        {
            Task.Run(async () =>
            {
                while (Monitoring)
                {
                    Monitor();

                    // don't run again for at least X milliseconds
                    await Task.Delay(monitorIntervalMilliseconds).ConfigureAwait(false);
                }
            });
        }

        private void MonitorOnce()
        {
            Monitor();
        }

        private void Monitor()
        {
            var sentinels = GetServers();
            foreach (var sentinel in sentinels)
            {
                if (sentinel.IsConnected)
                {
                    var masterEndpoint = sentinel.SentinelGetMasterAddressByName(serviceName, CommandFlags.None);
                    var sentinelEndpoint = sentinel.EndPoint;
                    log.Debug($"Monitor Master: Sentinel:{sentinelEndpoint}, Report Master:{masterEndpoint}");
                    if (master == null)
                    {
                        master = masterEndpoint;
                        log.Debug($"Initialize Master: {master}");
                    }
                    else if (!masterEndpoint.ToString().Equals(master.ToString()))
                    {
                        master = masterEndpoint;
                        log.Debug("Master Changed: {0}", master);
                        onMasterChanged(masterEndpoint);
                        break;
                    }
                }
            }
        }
    }
}
