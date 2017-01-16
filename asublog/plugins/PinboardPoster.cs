namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Asublog.Core;
    using TinyJson;

    class JsonPinboardPostCollection
    {
        public string date { get; set; }
        public string user { get; set; }
        public List<JsonPinboardPost> posts { get; set; }
    }

    class JsonPinboardPost
    {
        public string href { get; set; }
        public string description { get; set; }
        public string extended { get; set; }
        public string meta { get; set; }
        public string hash { get; set; }
        public string time { get; set; }
        public string shared { get; set; }
        public string toread { get; set; }
        public string tags { get; set; }

        public PinboardPost ToPost()
        {
            return new PinboardPost
            {
                Url = href,
                Name = description,
                Description = !string.IsNullOrEmpty(extended)
                    ? extended
                    : null,
                Date = DateTime.Parse(time)
            };
        }
    }

    class PinboardPost
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
    }

    public class PinboardPoster : PostingPlugin
    {
        private static volatile int _skipCount = 0;
        private static volatile int _skipped = 0;

        public override int PingInterval
        {
            get { return int.Parse(Config["interval"]); }
        }

        public PinboardPoster() : base("pinboardPoster", "0.5") { }

        private PinboardPost[] GetRecentPosts()
        {
            var req = (HttpWebRequest) WebRequest.Create(string.Format("https://api.pinboard.in/v1/posts/recent/?auth_token={0}:{1}&tag={2}&format=json", Config["user"], Config["userToken"], Config["tag"]));

            try
            {
                var res = (HttpWebResponse) req.GetResponse();
                if(res.StatusCode == HttpStatusCode.OK)
                {
                    using(var sr = new StreamReader(res.GetResponseStream()))
                    {
                        var json = sr.ReadToEnd();
                        Log.Debug("Pinboard Json", json);

                        if(json.Contains(@"""posts"":[]"))
                            return new PinboardPost[] {};

                        var collection = json.FromJson<JsonPinboardPostCollection>();
                        Log.Debug("Parsed pinboard posts", collection);
                        _skipped = 0;
                        return collection.posts.Select(p => p.ToPost()).ToArray();
                    }
                }
                else
                {
                    throw new Exception(string.Format("Unexpected response from pinboard: {0}", res.StatusCode));
                }
            }
            catch(WebException)
            {
                _skipCount = ++_skipped;
                Log.Error(string.Format("Pinboard request failure, skipping next {0} pings", _skipCount));
                return new PinboardPost[] {};
            }
        }

        public override void Ping()
        {
            if(_skipCount > 0)
            {
                Log.Info("Skipping ping due to pinboard errors");
                _skipCount--;
                return;
            }

            var posts = GetRecentPosts();
            for(var i = posts.Length - 1; i >= 0; i--)
            {
                var post = posts[i];
                if(App.CacheGet(post.Url) == null)
                {
                    Log.Debug("New pinboard post detected", post);
                    App.CacheSet(post.Url, "1");
                    App.ReceivePost(new Post
                    {
                        Created = post.Date,
                        Source = "pinboard",
                        Content = string.Format("{0} {1}{2}",
                            post.Name, post.Url,
                            !string.IsNullOrEmpty(post.Description)
                                ? string.Format(" - {0}", post.Description)
                                : null)
                    });
                }
            }
        }
    }
}
