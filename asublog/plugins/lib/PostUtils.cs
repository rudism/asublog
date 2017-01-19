namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using Core;

    public static class PostUtils
    {
        private static readonly Dictionary<string, string> _pageContent = new Dictionary<string, string>();
        private static readonly object _pageLock = new object();

        public static readonly Regex UrlRegex =
            new Regex(@"(?<=(^|\s))(www\.|https?://)([^\s]+)(?=(\s|\.(\s|$)|,\s|$))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _hasproto =
            new Regex(@"^https?://",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _excess =
            new Regex(@"^(https?://)?(www\.)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ensure it has protocol
        public static string NormalizeUrl(string url)
        {
            return _hasproto.IsMatch(url)
                ? url
                : string.Format("http://{0}", url);
        }

        // strip the protocol if there is one
        public static string SanitizeUrl(string url)
        {
            return _hasproto.IsMatch(url)
                ? _excess.Replace(url, string.Empty)
                : url;
        }

        public static string GetPageContent(ILogger log, string url)
        {
            lock(_pageLock)
            {
                if(_pageContent.ContainsKey(url))
                {
                    log.Debug(string.Format("Got cached page content for {0}", url));
                    return _pageContent[url];
                }

                try
                {
                    log.Debug(string.Format("Downloading page content at {0}", url));
                    var req = (HttpWebRequest) WebRequest.Create(url);
                    req.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1";
                    using(var resp = (HttpWebResponse) req.GetResponse())
                    {
                        using(var sr = new StreamReader(resp.GetResponseStream()))
                        {
                            var content = sr.ReadToEnd();
                            _pageContent.Add(url, content);
                            return content;
                        }
                    }
                }
                catch(Exception ex)
                {
                    log.Error(string.Format("Error downloading link {0}", url), ex);
                }
                return null;
            }
        }

        public static void ResetPageContentStore()
        {
            lock(_pageLock) _pageContent.Clear();
        }
    }
}
