namespace Asublog.Plugins
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using YamlDotNet.Serialization;
    using Core;

    public class ConsolePublisher : PublishingPlugin
    {
        public ConsolePublisher() : base("consolePublisher", "1.0") { }

        public override void Publish(IEnumerator<Post> posts, int count)
        {
            Post latest = null;
            while(posts.MoveNext())
            {
                if(latest == null || posts.Current.Created > latest.Created)
                {
                    latest = posts.Current;
                }
            }

            var sb = new StringBuilder();
            var serializer = new Serializer();
            serializer.Serialize(new IndentedTextWriter(new StringWriter(sb)), latest);
            Log.Info(string.Format("New post:\n{0}", sb));
        }
    }
}
