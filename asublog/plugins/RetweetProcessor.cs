namespace Asublog.Plugins
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using Core;

    public class RetweetProcessor : ProcessingPlugin
    {
        private static readonly Regex _twitter =
            new Regex(@"(?<=(^|\s))https?://((www|mobile)\.)?twitter\.com/(?<user>[a-zA-Z0-9_]+)/status/(?<id>\d+)(?=([^\d]|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _ogdata =
            new Regex(@"<meta\s+property\s*=\s*""og:(?<field>[a-zA-Z0-9_:]+)""\s+content\s*=\s*""(?<content>[^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public RetweetProcessor() : base("retweetProcessor", "0.5") { }

        public override void Process(Post post)
        {
            // look for twitter links
            var urls = _twitter.Matches(post.Content);
            foreach(Match match in urls)
            {
                var url = match.Value.Replace("//mobile.", "//");
                var user = match.Groups["user"].Value;
                var id = match.Groups["id"].Value;

                Log.Debug(string.Format("Found twitter url {0}", url));

                var tweet = App.CacheGet(id);

                if(string.IsNullOrEmpty(tweet))
                {
                    var content = PostUtils.GetPageContent(Log, url);

                    var ogdata = new Dictionary<string, string>();
                    var ogs = _ogdata.Matches(content);
                    foreach(Match og in ogs)
                    {
                        var ogfield = og.Groups["field"].Value;
                        var ogcontent = og.Groups["content"].Value;
                        ogdata.Add(ogfield, ogcontent);
                    }

                    Log.Debug("Extracted tweet data", ogdata);

                    if(ogdata.ContainsKey("type")
                        && ogdata["type"] == "article"
                        && ogdata.ContainsKey("description"))
                    {
                        tweet = string.Format("{0} - @{1}",
                            Regex.Unescape(ogdata["description"]), user);

                        App.CacheSet(id, tweet);
                    }
                }
                if(!string.IsNullOrEmpty(tweet))
                {
                    post.Attach("tweet", url, tweet, true);
                }
            }
        }
    }
}
