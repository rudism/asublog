namespace Asublog.Core
{
    using System;
    using Plugins;

    public interface ILogger
    {
        LoggingPlugin[] Loggers { get; set; }
        void Info(string msg);
        void Error(string msg, Exception ex = null);
        void Debug(string msg, object obj = null);
    }

    public class Logger : ILogger
    {
        private static readonly object _lock = new object();

        public LoggingPlugin[] Loggers { get; set; }

        public void Info(string msg)
        {
            lock(_lock)
            {
                foreach(var logger in Loggers)
                {
                    logger.Info(msg);
                }
            }
        }

        public void Error(string msg, Exception ex = null)
        {
            lock(_lock)
            {
                foreach(var logger in Loggers)
                {
                    logger.Error(msg, ex);
                }
            }
        }

        public void Debug(string msg, object obj = null)
        {
            lock(_lock)
            {
                foreach(var logger in Loggers)
                {
                    logger.Debug(msg, obj);
                }
            }
        }
    }
}
