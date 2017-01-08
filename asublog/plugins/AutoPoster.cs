namespace Asublog.Plugins
{
    using Core;

    public class AutoPoster : PostingPlugin
    {
        public override int PingInterval
        {
            get { return int.Parse(Config["interval"]); }
        }

        public AutoPoster() : base("autoPoster", "1.0") { }

        public override void Ping()
        {
            Log.Info("Creating auto post");
            App.ReceivePost(new Post
            {
                Content = Config["content"]
            });
        }
    }
}
