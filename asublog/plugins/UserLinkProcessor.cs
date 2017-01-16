namespace Asublog.Plugins
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;

    public class UserLinkProcessor : ProcessingPlugin
    {
        private static readonly Regex _userLinks =
            new Regex(@"(?<=(^|\s))(@[a-zA-Z0-9_]+)(?=([^a-zA-Z0-9_]|$))", RegexOptions.Compiled);

        public UserLinkProcessor() : base("userLinkProcessor", "0.5") { }

        private string Process(string content)
        {
            var autoTwitter = Config["_autoTwitter"] == "true";

            var processed = content;

            var matches = _userLinks.Matches(content);
            foreach(Match match in matches)
            {
                var handle = match.Value;
                var name = handle.TrimStart(new[] {'@'});

                var link = Config[name.ToLower()];

                if(link == null && autoTwitter)
                    link = string.Format("https://twitter.com/{0}", name);

                if(link != null)
                {
                    processed = processed.Replace(handle,
                        string.Format("<a href='{0}'>{1}</a>", link, handle));
                }
            }

            return processed;
        }

        public override void Process(Post post)
        {
            post.Content = Process(post.Content);
            foreach(var attachment in post.Attachments.Where(a => a.ShouldProcess))
            {
                attachment.Content = Process(attachment.Content);
            }
        }
    }
}
