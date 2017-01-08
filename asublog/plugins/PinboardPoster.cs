namespace Asublog.Plugins
{
    public class PinboardPoster : PostingPlugin
    {
        public override int PingInterval { get { return 0; } }

        public PinboardPoster() : base("pinboardPoster", "0.5") { }

        public override void Ping()
        {
        }
    }
}
