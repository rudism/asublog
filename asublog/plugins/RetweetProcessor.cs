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
            new Regex(@"<meta\s+property\s*=\s*""og:(?<field>[a-zA-Z0-9_]+)""\s+content\s*=\s*""(?<content>[^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public RetweetProcessor() : base("retweetProcessor", "0.5") { }

        public override void Process(Post post)
        {
            // look for twitter links
            var urls = _twitter.Matches(post.Content);
            foreach(Match match in urls)
            {
                var url = match.Value;
                var user = match.Groups["user"].Value;
                var id = match.Groups["id"].Value;

                Log.Debug(string.Format("Found twitter url {0}", url));

                var tweet = App.CacheGet(id);
                var imgKey = string.Format("img-{0}", id);
                var image = App.CacheGet(imgKey);

                if(string.IsNullOrEmpty(tweet))
                {
                    var req = (HttpWebRequest) WebRequest.Create(url);
                    using(var resp = req.GetResponse())
                    {
                        var sr = new StreamReader(resp.GetResponseStream());
                        var content = sr.ReadToEnd();

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

                            if(ogdata.ContainsKey("image"))
                            {
                                image = ogdata["image"];
                                App.CacheSet(imgKey, image);
                            }
                        }
                    }
                }
                if(!string.IsNullOrEmpty(tweet))
                {
                    var nonMobileUrl = url.Replace("//mobile.", "//");
                    post.Attach("tweet", nonMobileUrl, tweet, true);
                }
                if(!string.IsNullOrEmpty(image))
                {
                    post.Attach("image", url, image);
                }
            }
        }
    }
}
