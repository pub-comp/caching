using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    internal class LogHelper
    {
        //Initialize default Logger
        private static readonly Logger m_log;

        static LogHelper()
        {
            //Create configuration object in code
            string defaultLayout = "${date}::${logger}::${message}";
            LoggingConfiguration config = new LoggingConfiguration();

            // debugging target
            string debuggerTargetName = "Redis.StackExchange.Logger.DebuggerTarget";
            config.AddTarget(new DebuggerTarget(debuggerTargetName) {Layout = defaultLayout });
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debuggerTargetName);

            // init logger
            LogManager.Configuration = config;
            m_log = LogManager.GetLogger("Redis.StackExchange.Logger");
        }
        
        public static Logger Log
        {
            get { return m_log; }
        }
    }
}
