namespace Asublog.Plugins
{
    using System.Text.RegularExpressions;
    using Core;

    public class UserLinkProcessor : ProcessingPlugin
    {
        private static readonly Regex _userLinks =
            new Regex(@"(?<=(^|\s))(@[a-zA-Z0-9_]+)(?=([^a-zA-Z0-9_]|$))", RegexOptions.Compiled);

        public UserLinkProcessor() : base("userLinkProcessor", "0.5") { }

        public override void Process(Post post)
        {
            var autoTwitter = Config["_autoTwitter"] == "true";

            var matches = _userLinks.Matches(post.Content);
            foreach(Match match in matches)
            {
                var handle = match.Value;
                var name = handle.TrimStart(new[] {'@'});

                var link = Config[name.ToLower()];

                if(link == null && autoTwitter)
                    link = string.Format("https://twitter.com/{0}", name);

                if(link != null)
                {
                    post.Content = post.Content.Replace(handle,
                        string.Format("<a href='{0}'>{1}</a>", link, handle));
                }
            }
        }
    }
}
