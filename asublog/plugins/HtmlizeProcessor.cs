namespace Asublog.Plugins
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;

    public class HtmlizeProcessor : ProcessingPlugin
    {
        private static readonly Regex _urls =
            new Regex(@"(?<=(^|\s))(www\.|https?://)([^\s]+)(?=(\s|\.(\s|$)|,\s|$))",
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

        private string Process(string content)
        {
            string processed = content;

            var urls = _urls.Matches(content);
            foreach(Match match in urls)
            {
                var url = match.Value;
                if(!_hasproto.IsMatch(url))
                {
                    url = string.Format("http://{0}", url);
                }
                var noproto = _excess.Replace(url, string.Empty);

                processed = processed.Replace(match.Value,
                    string.Format("<a href='{0}'>{1}</a>", url, noproto));
            }

            // line breaks
            if(Config["allowNewlines"] == "true")
            {
                processed = _newlines.Replace(processed, "<br>");
            }

            return processed;
        }

        public override void Process(Post post)
        {
            Log.Debug("Htmlizing post");
            post.Content = Process(post.Content);
            foreach(var attachment in post.Attachments.Where(a => a.ShouldProcess))
            {
                attachment.Content = Process(attachment.Content);
            }
        }
    }
}
