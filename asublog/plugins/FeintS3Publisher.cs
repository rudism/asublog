namespace Asublog.Plugins
{
    using System.Collections.Generic;
    using Core;

    public class FeintS3Publisher : PublishingPlugin
    {
        public FeintS3Publisher() : base("feintS3Publisher", "0.5") { }

        public override void Publish(IEnumerator<Post> posts)
        {
        }
    }
}
