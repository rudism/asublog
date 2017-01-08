namespace Asublog.Plugins
{
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Text;
    using YamlDotNet.Serialization;
    using Core;

    public class ConsoleSaver : SavingPlugin
    {
        public ConsoleSaver() : base("consoleSaver", "1.0") { }

        public override void Save(Post post)
        {
            var sb = new StringBuilder();
            var serializer = new Serializer();
            serializer.Serialize(new IndentedTextWriter(new StringWriter(sb)), post);
            Log.Info(string.Format("New post:\n{0}", sb));
        }

        public override void Flush() { }
    }
}
