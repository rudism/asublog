namespace Asublog.Plugins
{
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using Core;

    public class LilurlProcessor : ProcessingPlugin
    {
        private static readonly Regex _urls =
            new Regex(@"(?<=(^|\s))(www\.|https?://)([^\s]+)(?=(\s|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _lilerr =
            new Regex(@"<p class=""error"">(?<msg>[^<]+)</p>",
            RegexOptions.Compiled);

        private static readonly Regex _lilok =
            new Regex(@"<p class=""success"">[^<]+<a href=""(?<lilurl>[^""]+)""",
            RegexOptions.Compiled);

        private static readonly Regex _hasproto =
            new Regex(@"^https?://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LilurlProcessor() : base("lilurlProcessor", "0.5") { }

        public override void Process(Post post)
        {
            var urls = _urls.Matches(post.Content);
            foreach(Match match in urls)
            {
                var url = match.Value;
                if(!_hasproto.IsMatch(url))
                {
                    url = string.Format("http://{0}", url);
                }
                var req = (HttpWebRequest) WebRequest.Create(Config["url"]);
                req.AllowWriteStreamBuffering = false;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                var data = HttpUtility.ParseQueryString(string.Empty);
                data.Add("longurl", url);
                var postData = Encoding.UTF8.GetBytes(data.ToString());
                req.ContentLength = postData.Length;
                using(var stream = req.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                    stream.Flush();
                    stream.Close();
                }
                Log.Debug(string.Format("Getting lilUrl for {0}", url));
                using(var resp = (HttpWebResponse) req.GetResponse())
                {
                    var sr = new StreamReader(resp.GetResponseStream());
                    var content = sr.ReadToEnd();
                    var lilmatch = _lilok.Match(content);
                    if(lilmatch.Success)
                    {
                        var newUrl = lilmatch.Groups["lilurl"].Value;
                        Log.Debug(string.Format("Got new lilurl {0}", newUrl));
                        post.Content = post.Content.Replace(match.Value, newUrl);
                    }
                    else
                    {
                        lilmatch = _lilerr.Match(content);
                        if(lilmatch.Success)
                        {
                            var msg = lilmatch.Groups["msg"].Value;
                            Log.Error(string.Format("Lilurl error for {0}: {1}", url, msg));
                        }
                        else
                        {
                            Log.Error(string.Format("Unexpected lilurl response for {0}: {1}", url, resp.StatusCode));
                        }
                    }
                }
            }
        }
    }
}
