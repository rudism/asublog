namespace Asublog.Plugins
{
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using Core;

    public class ImageProcessor : ProcessingPlugin
    {
        private static readonly Regex _dropbox =
            new Regex(@"(?<=(^|\s))https?://(www\.)?dropbox\.com/[a-z]+/[^\s]+(?=(\s|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _imgsrc =
            new Regex(@"\simg.src = ""(?<src>[^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ImageProcessor() : base("imageProcessor", "0.5") { }

        public override void Process(Post post)
        {
            // handle shared dropbox photos
            var urls = _dropbox.Matches(post.Content);
            foreach(Match match in urls)
            {
                var url = match.Value;
                var imgurl = App.CacheGet(url);
                if(string.IsNullOrEmpty(imgurl))
                {
                    Log.Debug(string.Format("Checking dropbox url for image {0}", url));
                    var req = (HttpWebRequest) WebRequest.Create(url);
                    req.AllowWriteStreamBuffering = false;
                    using(var resp = req.GetResponse())
                    {
                        var sr = new StreamReader(resp.GetResponseStream());
                        var content = sr.ReadToEnd();

                        var imgmatch = _imgsrc.Match(content);
                        if(imgmatch.Success)
                        {
                            imgurl = Regex.Unescape(imgmatch.Groups["src"].Value);
                            App.CacheSet(url, imgurl);
                            Log.Debug(string.Format("Found image url {0}", imgurl));
                        }
                        else
                        {
                            Log.Debug(string.Format("No image found at {0}", url));
                        }
                    }
                }
                if(!string.IsNullOrEmpty(imgurl))
                {
                    post.Attach("image", url, imgurl);
                }
            }
        }
    }
}
