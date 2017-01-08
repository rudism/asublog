namespace Asublog.Plugins
{
    using System;
    using Sharp.Xmpp.Client;
    using Sharp.Xmpp.Im;
    using Core;

    public class XmppPoster : PostingPlugin
    {
        private XmppClient _client;

        public XmppPoster() : base("xmppPoster", "0.5") { }

        public override void Init()
        {
            _client = new XmppClient(Config["host"], Config["jid"], Config["password"]);
            _client.Error += (sender, e) => Log.Error("XMPP error", e.Exception);
            _client.Message += (sender, e) =>
            {
                Log.Debug("XMPP message received", e.Message);
                if(e.Message.Type == MessageType.Chat)
                {
                    if(e.Message.From.ToString().StartsWith(string.Format("{0}/", Config["authorized"]), StringComparison.OrdinalIgnoreCase))
                    {
                        var post = new Post
                        {
                            Content = e.Message.Body
                        };
                        App.ReceivePost(post);
                    }
                    else
                    {
                        Log.Info(string.Format("XMPP received chat from unauthorized jid {0}: {1}", e.Message.From, e.Message.Body));
                    }
                }
            };

            Log.Info("XMPP connecting");
            _client.Connect();
        }

        public override void Dispose()
        {
            if(_client.Connected)
            {
                Log.Info("XMPP disconnecting");
                _client.Close();
            }
        }
    }
}
