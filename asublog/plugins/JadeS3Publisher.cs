namespace Asublog.Plugins
{
    using System.Collections.Generic;
    using Core;

    public class JadeS3Publisher : PublishingPlugin
    {
        public JadeS3Publisher() : base("jadeS3Publisher", "0.5") { }

        public void Publish(IEnumerator<Post> posts)
        {
        }
    }
}
