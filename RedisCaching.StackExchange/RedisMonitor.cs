using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
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
        void StartMonitor(ConfigurationOptions config, int monitorPort, int monitorIntervalMilliseconds,Func<IPEndPoint, bool> masterChanged);
        void StopMonitor();
        IPEndPoint MasterEndpoint { get; }
    }

    internal class RedisReplicaMonitor : IRedisMonitor
    {
        private IConnectionMultiplexer _innerConnection;
        private IPEndPoint _master;
        private Func<IPEndPoint, bool> _masterChanged;
        private int Port = 26379;
        private string ServiceName = "mymaster";

        private bool Monitoring { get; set; }

        public IPEndPoint MasterEndpoint
        {
            get
            {
                return _master;
            }
        }

        public void StartMonitor(ConfigurationOptions config, int monitorPort, int monitorIntervalMilliseconds,
            Func<IPEndPoint, bool> masterChanged)
        {
            try
            {
                Port = monitorPort;
                ServiceName = config.ServiceName;
                _innerConnection = GetConnection(config);
                _masterChanged = masterChanged;
                Monitoring = true;
                MonitorOnce();
                //MonitorPull(monitorIntervalMilliseconds);
                MonitorPush();
            }
            catch (Exception exp)
            {
                LogHelper.Log.Fatal("No master found. Reason:{0}", exp.Message);
                throw new EntryPointNotFoundException("No master found");
            }
        }

        public void StopMonitor()
        {
            Monitoring = false;
        }

        private IEnumerable<IServer> GetServers()
        {
            var servers = _innerConnection.GetEndPoints(false)
                .Select(ep => _innerConnection.GetServer(ep)).ToList();
            return servers;
        }

        private ConnectionMultiplexer GetConnection(ConfigurationOptions config)
        {
            // create a connection
            var options = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                AllowAdmin = true,
                TieBreaker = "",
                ServiceName = ServiceName,
                SyncTimeout = 5000
            };

            IEnumerable<IPAddress> sentinels = config.EndPoints.Select(ep => (ep as IPEndPoint).Address).ToList();
            foreach (var ipAddress in sentinels)
            {
                options.EndPoints.Add(ipAddress, Port);
            }

            var connection = ConnectionMultiplexer.Connect(options, Console.Out);
            return connection;
        }

        private void MonitorPush()
        {
            _innerConnection.GetSubscriber(ServiceName).SubscribeAsync("+switch-master", (channel, message) =>
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
                    await Task.Delay(monitorIntervalMilliseconds);
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
                    var masterEndpoint = (IPEndPoint) sentinel.SentinelGetMasterAddressByName(ServiceName, CommandFlags.None);
                    var sentinelEndpoint = (IPEndPoint) sentinel.EndPoint;
                    LogHelper.Log.Info("Monitor Master: Sentinel:{0}, Report Master:{1}", sentinelEndpoint.Address, masterEndpoint.Address);
                    if (_master == null)
                    {
                        _master = masterEndpoint;
                        LogHelper.Log.Info("Init Master: {0}", _master.Address);
                    }
                    else if (!masterEndpoint.Address.ToString().Equals(_master.Address.ToString()))
                    {
                        _master = masterEndpoint;
                        LogHelper.Log.Warn("Master Changed: {0}", _master.Address);
                        _masterChanged(masterEndpoint);
                        break;
                    }
                }
            }
        }
    }
}
