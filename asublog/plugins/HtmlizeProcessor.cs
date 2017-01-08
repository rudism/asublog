namespace Asublog.Plugins
{
    using System.Text.RegularExpressions;
    using Core;

    public class HtmlizeProcessor : ProcessingPlugin
    {
        private static readonly Regex _urls =
            new Regex(@"(?<=(^|\s))(www\.|https?://)([^\s]+)(?=(\s|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _hasproto =
            new Regex(@"^https?://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _excess =
            new Regex(@"^(https?://)?(www\.)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _newlines =
            new Regex(@"(\r?\n)", RegexOptions.Compiled);

        public HtmlizeProcessor() : base("htmlizeProcessor", "0.5") { }

        public override void Process(Post post)
        {
            Log.Debug("Htmlizing post");
            // hyperlink urls
            var urls = _urls.Matches(post.Content);
            foreach(Match match in urls)
            {
                var url = match.Value;
                if(!_hasproto.IsMatch(url))
                {
                    url = string.Format("http://{0}", url);
                }
                var noproto = _excess.Replace(url, string.Empty);

                post.Content = post.Content.Replace(url,
                    string.Format("<a href='{0}'>{1}</a>", url, noproto));
            }

            // line breaks
            if(Config["allowNewlines"] == "true")
            {
                post.Content = _newlines.Replace(post.Content, "<br>");
            }
        }
    }
}
