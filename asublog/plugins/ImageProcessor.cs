namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using Core;

    public class ImageProcessor : ProcessingPlugin
    {
        private static readonly Regex _ogimage =
            new Regex(@"<meta\s[^>]*property\s*=\s*(""|')og:image[^>]+>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _metaname =
            new Regex(@"\sproperty\s*=\s*(""|')(?<property>.+?)(?=\1)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _metacontent =
            new Regex(@"\scontent\s*=\s*(""|')(?<content>.+?)(?=\1)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _twitter =
            new Regex(@"^https?://([^\.]+\.)*twitter\.com\/",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private int _minWidth;
        private int _minHeight;
        private int _maxWidth;

        private S3Util _client;

        public ImageProcessor() : base("imageProcessor", "0.9") { }

        public override void Init()
        {
            _minWidth = int.Parse(Config["minWidth"]);
            _minHeight = int.Parse(Config["minHeight"]);
            _maxWidth = int.Parse(Config["maxWidth"]);

            _client = new S3Util(Config["awsKey"], Config["awsSecret"], Config["awsRegion"]) { Log = Log };
        }

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            Log.Debug(string.Format("Resizing image from {0}x{1} to {2}x{3}", image.Width, image.Height, width, height));
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            if(image.HorizontalResolution > 0 && image.VerticalResolution > 0)
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width,image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public override void Process(Post post)
        {
            // download linked url, look for og:image meta tag
            var urls = PostUtils.UrlRegex.Matches(post.Content);
            foreach(Match murl in urls)
            {
                var url = PostUtils.NormalizeUrl(murl.Value);

                var cachedImgUrl = App.CacheGet(url);
                if(cachedImgUrl != null)
                {
                    if(!cachedImgUrl.Equals(":none"))
                    {
                        Log.Debug(string.Format("Found cached image at {0}", cachedImgUrl));
                        post.Attach("image", url, cachedImgUrl);
                    }
                    break;
                }

                Log.Debug(string.Format("Checking {0} for opengraph image", url));

                var properties = new Dictionary<string, string>();
                var content = PostUtils.GetPageContent(Log, url);
                var metas = _ogimage.Matches(content);
                foreach(Match meta in metas)
                {
                    string prop = null;
                    string val = null;

                    var mprop = _metaname.Match(meta.Value);
                    if(mprop.Success)
                        prop = WebUtility.HtmlDecode(mprop.Groups["property"].Value.ToLower());
                    var mcont = _metacontent.Match(meta.Value);
                    if(mcont.Success)
                        val = WebUtility.HtmlDecode(mcont.Groups["content"].Value);

                    if(!string.IsNullOrEmpty(prop) && !string.IsNullOrEmpty(val) && !properties.ContainsKey(prop))
                        properties.Add(prop, val);
                }

                if(properties.Count == 0 || !properties.ContainsKey("og:image"))
                {
                    App.CacheSet(url, ":none");
                    continue;
                }

                string imgurl = null;
                Log.Debug("Got image meta properties", properties);
                // if it's twitter we only want user generated images
                if(!_twitter.IsMatch(url) || (properties.ContainsKey("og:image:user_generated") && properties["og:image:user_generated"] == "true"))
                {
                    imgurl = properties["og:image"];
                }

                if(string.IsNullOrWhiteSpace(imgurl))
                {
                    App.CacheSet(url, ":none");
                    continue;
                }

                try
                {
                    var uri = new Uri(imgurl);
                    var fname = Path.GetFileName(uri.AbsolutePath);

                    var wc = new WebClient();
                    var tpath = Path.Combine(Config["tempDir"], fname);

                    Log.Debug(string.Format("Downloading image {0} to {1}", imgurl, tpath));

                    wc.DownloadFile(imgurl, tpath);
                    using(var bitmap = Image.FromFile(tpath))
                    {
                        if(bitmap.Width < _minWidth
                            || bitmap.Height < _minHeight)
                        {
                            Log.Debug(string.Format("Ignoring image {0}, too small ({1}x{2})", fname, bitmap.Width, bitmap.Height));
                            App.CacheSet(url, ":none");
                            File.Delete(tpath);
                            continue;
                        }

                        var type = bitmap.RawFormat;
                        if(!type.Equals(ImageFormat.Jpeg) 
                            && !type.Equals(ImageFormat.Gif)
                            && !type.Equals(ImageFormat.Png))
                        {
                            Log.Debug(string.Format("Ignoring image {0}, unsupported type ({1})", fname, bitmap.RawFormat));
                            App.CacheSet(url, ":none");
                            File.Delete(tpath);
                            continue;
                        }

                        if(bitmap.Width > _maxWidth)
                        {
                            using(var resized = ResizeImage(bitmap, _maxWidth, (int) Math.Round(bitmap.Height / (float) bitmap.Width * _maxWidth)))
                                resized.Save(tpath, type);
                        }

                        string extension;
                        if(type.Equals(ImageFormat.Jpeg)) extension = "jpg";
                        else if(type.Equals(ImageFormat.Png)) extension = "png";
                        else extension = "gif";

                        var uploadName = string.Format("{0}/{1}.{2}", Config["s3path"], post.Id, extension);

                        if(_client.UploadFile(Config["bucket"], tpath, uploadName) && Config["cloudfrontDistId"] != null)
                        {
                            _client.InvalidateCloudFrontDistribution(Config["cloudfrontDistId"], new List<string> {string.Format("/{0}", uploadName)});
                        }
                        // don't bother getting more than one image per post
                        var newImgUrl = string.Format("{0}/{1}", Config["siteUrl"], uploadName);
                        App.CacheSet(url, newImgUrl);
                        post.Attach("image", url, newImgUrl);
                        File.Delete(tpath);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(string.Format("Error processing image {0}", imgurl), ex);
                }
            }
        }
    }
}
