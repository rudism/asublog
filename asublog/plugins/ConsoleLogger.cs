namespace Asublog.Plugins
{
    using System;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Text;
    using YamlDotNet.Serialization;
    using Core;

    public class ConsoleLogger : LoggingPlugin
    {
        private static bool _debug = Environment.GetEnvironmentVariable("DEBUG") != null;

        public ConsoleLogger() : base("consoleLogger", "1.0") { }

        public override void Info(string msg)
        {
            Console.WriteLine("[{0}] INFO: {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
        }

        public override void Error(string msg, Exception ex = null)
        {
            Console.WriteLine("[{0}] ERROR: {1}\n", DateTime.Now.ToString("HH:mm:ss"), msg);
            var cur = ex;
            while(cur != null)
            {
                Console.WriteLine("Exception: {0}\nMessage: {1}\n{2}",
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                cur = cur.InnerException;
            }
        }

        public override void Debug(string msg, object obj = null)
        {
            if(_debug)
            {
                Console.WriteLine("[{0}] DEBUG: {1}", DateTime.Now.ToString("HH:mm:ss"), msg);
                if(obj != null)
                {
                    var sb = new StringBuilder();
                    var serializer = new Serializer();
                    serializer.Serialize(new IndentedTextWriter(new StringWriter(sb)), obj);
                    Console.WriteLine(sb);
                }
            }
        }
    }
}
