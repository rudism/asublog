namespace Asublog.Plugins
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;

    public class TcpPoster : PostingPlugin
    {
        private volatile bool _keepRunning;

        public TcpPoster() : base("tcpPoster", "0.5") { }

        public override void Init()
        {
            var thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            var port = int.Parse(Config["port"]);
            var server = new TcpListener(IPAddress.Any, port);
            try
            {
                server.Start();

                Log.Debug(string.Format("Tcp server listening on port {0}", port));
                _keepRunning = true;

                while(_keepRunning)
                {
                    if(!server.Pending())
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    var client = server.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
            }
            finally
            {
                server.Stop();
                Log.Debug("Tcp server stopped");
            }
        }

        private void HandleClient(object state)
        {
            using(var client = (TcpClient) state)
            {
                Log.Debug("Tcp server accepted connection");
                using(var reader = new StreamReader(client.GetStream()))
                {
                    var content = reader.ReadToEnd().Trim();
                    Log.Debug("Tcp server received content", content);
                    App.ReceivePost(new Post
                    {
                        Source = "tcpserver",
                        Content = content
                    });
                }
                client.Close();
            }
        }

        public override void Dispose()
        {
            _keepRunning = false;
        }
    }
}
