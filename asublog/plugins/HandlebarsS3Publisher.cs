namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Amazon;
    using Amazon.CloudFront;
    using Amazon.CloudFront.Model;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Transfer;
    using HandlebarsDotNet;
    using Core;

    public class ExtendedPost : Post
    {
        public IConfiguration Config { get; set; }

        public ExtendedPost(Post post)
        {
            Id = post.Id;
            Created = post.Created;
            Content = post.Content;
            Source = post.Source;
            Processed = post.Processed;
            Attachments = post.Attachments;
        }

        public new Attachment[] Attachments { get; set; }

        public bool HasImage
        {
            get { return Attachments.Any(a => a.Type == "image"); }
        }

        public Attachment Image
        {
            get { return Attachments.FirstOrDefault(a => a.Type == "image"); }
        }

        public bool HasTweet
        {
            get { return Attachments.Any(a => a.Type == "tweet"); }
        }

        public Attachment Tweet
        {
            get { return Attachments.FirstOrDefault(a => a.Type == "tweet"); }
        }

        public string UserName { get { return Config["userName"]; } }
    }

    public class HandlebarsPageData
    {
        public IConfiguration Config { get; set; }
        public IEnumerable<ExtendedPost> Posts { get; set; }
        public ExtendedPost Post { get; set; }
        public int TotalPosts { get; set; }
        public string TotalPostsFormatted
        {
            get { return string.Format("{0:n0}", TotalPosts); }
        }
        public int PageNum { get; set; }
        public int MaxPage { get; set; }
        public bool IsFirstPage { get { return PageNum == MaxPage; } }
        public bool IsLastPage { get { return PageNum == 0; } }
        public IEnumerable<string> RecentTags { get; set; }
        public IEnumerable<string> PopularTags { get; set; }
        public string Prefix { get; set; }
        public bool HasPages { get { return MaxPage > 0; } }
        public string PrevPage
        {
            get { return string.Format("{0}-{1}.html", Prefix, PageNum + 1); }
        }
        public string NextPage
        {
            get
            {
                return (PageNum - 1) == 0
                    ? string.Format("{0}.html", Prefix)
                    : string.Format("{0}-{1}.html", Prefix, PageNum - 1);
            }
        }
        public bool IsHashtag { get { return Prefix != "index"; } }
        public bool HasHashtags { get { return RecentTags != null && RecentTags.Count() > 0; } }

        public string SiteUrl { get { return Config["siteUrl"]; } }
        public string SiteName { get { return Config["siteName"]; } }
        public string SiteCreated { get { return Config["siteCreated"]; } }
        public string UserName { get { return Config["userName"]; } }
        public string UserLinkName { get { return Config["userLinkName"]; } }
        public string UserLinkUrl { get { return Config["userLinkUrl"]; } }
        public string SiteDescription { get { return Config["siteDescription"]; } }
        public string Location { get { return Config["location"]; } }
    }

    public class HandlebarsS3Publisher : PublishingPlugin
    {
        private static readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        private string _assetPath;
        private Func<object, string> _index;
        private Func<object, string> _post;
        private int _postsPerPage;
        private AmazonS3Client _client;
        private string _bucket;
        private static readonly Regex _hashtags = new Regex(@"(?<=(\s|^)#)(?<hashtag>[a-zA-Z0-9_]+)(?=([^a-zA-Z0-9_]|$))", RegexOptions.Compiled);

        public HandlebarsS3Publisher() : base("handlebarsS3Publisher", "0.5") { }

        public override void Init()
        {
            _bucket = Config["bucket"];
            var theme = Config["theme"];
            _postsPerPage = int.Parse(Config["postsPerPage"]);

            if(string.IsNullOrEmpty(theme))
                throw new ArgumentException("No theme was specified");

            Handlebars.RegisterHelper("tz_created",
            (writer, context, parameters) =>
            {
                var date = (DateTime) context.Created;
                var tz = Config["tz"];
                if(!string.IsNullOrEmpty(tz))
                {
                    var tzinfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                    if(tzinfo != null)
                    {
                        date = TimeZoneInfo.ConvertTimeFromUtc(date, tzinfo);
                    }
                }
                writer.WriteSafeString(date);
            });

            Handlebars.RegisterHelper("encode",
            (writer, context, parameters) =>
            {
                if(parameters.Length != 1)
                    throw new ArgumentException("escape helper must have exactly one argument");

                writer.WriteSafeString(WebUtility.HtmlEncode((string) parameters[0]));
            });

            var themePath = Path.Combine(_basePath, theme);
            _assetPath = Path.Combine(themePath, "assets");

            var index = Path.Combine(themePath, "index.handlebars");
            if(!File.Exists(index)) throw new FileNotFoundException("Could not find index template", index);
            _index = Handlebars.Compile(File.ReadAllText(index));

            var post = Path.Combine(themePath, "post.handlebars");
            if(File.Exists(post))
            {
                _post = Handlebars.Compile(File.ReadAllText(post));
            }
            else
            {
                Log.Info("Post template not found, post pages will not be generated.");
            }

            _client = new AmazonS3Client(Config["awsKey"], Config["awsSecret"],
                RegionEndpoint.GetBySystemName(Config["awsRegion"]));
        }

        private string SaveIndex(HandlebarsPageData data)
        {
            var fname = string.Format("{0}{1}.html", data.Prefix, data.PageNum > 0 ? string.Format("-{0}", data.PageNum) : null);

            var content = _index(data);

            Log.Debug(string.Format("Rendered index {0}", fname));
            return UploadContent(fname, content);
        }

        private string SavePost(HandlebarsPageData data)
        {
            if(_post == null) return null;

            var fname = string.Format("post-{0}.html", data.Post.Id);
            var content = _post(data);

            Log.Debug(string.Format("Rendered post {0}", fname));
            return UploadContent(fname, content);
        }

        private string Md5ToStr(byte[] hash)
        {
            var sb = new StringBuilder();
            for(var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private string GetETag(string key)
        {
            try
            {
                var headReq = new GetObjectMetadataRequest
                {
                    BucketName = _bucket,
                    Key = key
                };

                var head = _client.GetObjectMetadata(headReq);
                return head.ETag.Trim(new[] {'"'});
            }
            catch(AmazonS3Exception ex)
            {
                if(ex.StatusCode != HttpStatusCode.NotFound) throw;
            }
            return null;
        }

        private string UploadContent(string fname, string content)
        {
            string md5;
            using(var md5er = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = md5er.ComputeHash(bytes);
                md5 = Md5ToStr(hash);
            }

            var etag = GetETag(fname);

            Log.Debug(string.Format("Content md5 {0}, S3 etag {1}", md5, etag));
            if(!md5.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                var putReq = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = fname,
                    ContentBody = content
                };

                var resp = _client.PutObject(putReq);
                if(resp.HttpStatusCode == HttpStatusCode.OK)
                {
                    Log.Info(string.Format("Uploaded s3://{0}/{1}", _bucket, fname));
                }
                else
                {
                    Log.Error(string.Format("Got unexpected result when uploading s3://{0}/{1}: {2}", _bucket, fname, resp.HttpStatusCode));
                }
                return string.Format("/{0}", fname);
            }
            else
            {
                Log.Debug(string.Format("Skipping s3://{0}/{1} (file unchanged)", _bucket, fname));
            }
            return null;
        }

        private string UploadFile(string path, string dir)
        {
            string md5;
            using(var md5er = MD5.Create())
            {
                using(var stream = File.OpenRead(path))
                {
                    var hash = md5er.ComputeHash(stream);
                    md5 = Md5ToStr(hash);
                }
            }

            var key = string.Format("{0}/{1}", dir, Path.GetFileName(path));
            var etag = GetETag(key);

            if(!md5.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                var putReq = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    FilePath = path
                };

                var resp = _client.PutObject(putReq);
                if(resp.HttpStatusCode == HttpStatusCode.OK)
                {
                    Log.Info(string.Format("Uploaded s3://{0}/{1}", _bucket, key));
                    return string.Format("/{0}", key);
                }
                else
                {
                    Log.Error(string.Format("Got unexpected result when uploading s3://{0}/{1}: {2}", _bucket, key, resp.HttpStatusCode));
                }
            }
            else
            {
                Log.Debug(string.Format("Skipping s3://{0}/{1} (file unchanged)", _bucket, key));
            }
            return null;
        }

        private List<string> UploadDirectory(string path, string dir)
        {
            Log.Debug(string.Format("Uploading directory {0} ({1})", path, dir));
            var uploaded = new List<string>();
            var files = Directory.GetFiles(path);
            foreach(var file in files)
            {
                var fpath = Path.Combine(path, file);
                string key;
                if(!string.IsNullOrEmpty((key = UploadFile(fpath, dir))))
                    uploaded.Add(key);
            }
            var folders = Directory.GetDirectories(path);
            foreach(var folder in folders)
            {
                var newdir = string.Format("{0}/{1}", dir, Path.GetFileName(folder));
                var dpath = Path.Combine(path, folder);
                uploaded.AddRange(UploadDirectory(dpath, newdir));
            }
            return uploaded;
        }

        public void Hashtagify(Dictionary<string, List<ExtendedPost>> hashtags, ExtendedPost post)
        {
            Log.Debug(string.Format("Looking for hashtags in post {0}", post.Id), post);
            var matches = _hashtags.Matches(post.Content);
            foreach(Match match in matches)
            {
                var hashtag = match.Value.ToLower();
                Log.Debug(string.Format("Found hashtag {0} in post {1}", hashtag, post.Id));
                if(!hashtags.ContainsKey(hashtag))
                    hashtags.Add(hashtag, new List<ExtendedPost>());
                if(!hashtags[hashtag].Contains(post))
                    hashtags[hashtag].Add(post);

                post.Content = post.Content.Replace(string.Format("#{0}", match.Value), string.Format("<a href='/{0}.html'>#{1}</a>", hashtag, match.Value));
            }
        }

        public override void Publish(IEnumerator<Post> posts, int count)
        {
            // git all hashtags from all posts to build recent/popular lists
            List<string> recentHashtags = null;
            Dictionary<string, int> popularHashtags = null;
            if(Config["hashtags"] == "true")
            {
                recentHashtags = new List<string>();
                popularHashtags = new Dictionary<string, int>();
                while(posts.MoveNext())
                {
                    var matches = _hashtags.Matches(posts.Current.Content);
                    foreach(Match match in matches)
                    {
                        var hashtag = match.Value.ToLower();
                        if(!recentHashtags.Contains(hashtag))
                            recentHashtags.Add(hashtag);
                        if(!popularHashtags.ContainsKey(hashtag))
                            popularHashtags.Add(hashtag, 0);
                        popularHashtags[hashtag] += 1;
                    }
                }
            }

            posts.Reset();

            var invalidations = new List<string> {"/"};

            var pagePosts = new List<ExtendedPost>();
            var hashtags = new Dictionary<string, List<ExtendedPost>>();
            var data = new HandlebarsPageData
            {
                Config = Config,
                TotalPosts = count,
                MaxPage = (int) Math.Ceiling((float) count / _postsPerPage) - 1,
                PageNum = 0,
                Prefix = "index",
                RecentTags = recentHashtags,
                PopularTags = popularHashtags != null
                    ? popularHashtags.OrderByDescending(t => t.Value).Select(t => t.Key).ToArray()
                    : null
            };
            while(posts.MoveNext())
            {
                var clone = new ExtendedPost((Post) posts.Current.Clone()) { Config = Config };
                if(Config["hashtags"] == "true")
                    Hashtagify(hashtags, clone);

                data.Post = clone;
                invalidations.Add(SavePost(data));
                pagePosts.Add(clone);
                if(pagePosts.Count == _postsPerPage)
                {
                    data.Posts = pagePosts.ToArray();
                    invalidations.Add(SaveIndex(data));
                    data.PageNum += 1;
                    pagePosts.Clear();
                }
            }
            if(pagePosts.Count > 0)
            {
                data.Posts = pagePosts.ToArray();
                invalidations.Add(SaveIndex(data));
            }

            foreach(var hashtag in hashtags.Keys)
            {
                var hashData = new HandlebarsPageData
                {
                    Config = Config,
                    TotalPosts = hashtags[hashtag].Count,
                    MaxPage = (int) Math.Ceiling((float) hashtags[hashtag].Count / _postsPerPage) - 1,
                    PageNum = 0,
                    Prefix = hashtag,
                    RecentTags = recentHashtags,
                    PopularTags = popularHashtags != null
                        ? popularHashtags.OrderByDescending(t => t.Value).Select(t => t.Key).ToArray()
                        : null
                };
                pagePosts.Clear();
                foreach(var post in hashtags[hashtag])
                {
                    pagePosts.Add(post);
                    if(pagePosts.Count == _postsPerPage)
                    {
                        hashData.Posts = pagePosts.ToArray();
                        invalidations.Add(SaveIndex(hashData));
                        hashData.PageNum += 1;
                        pagePosts.Clear();
                    }
                }
                if(pagePosts.Count > 0)
                {
                    hashData.Posts = pagePosts.ToArray();
                    invalidations.Add(SaveIndex(hashData));
                }
            }

            var invalidPaths = invalidations.Where(i => !string.IsNullOrEmpty(i)).ToList();

            invalidPaths.AddRange(UploadDirectory(_assetPath, "assets"));

            if(invalidPaths.Count > 0)
            {
                var distId = Config["cloudfrontDistId"];
                if(distId != null)
                {
                    var client = new AmazonCloudFrontClient(Config["awsKey"], Config["awsSecret"],
                        RegionEndpoint.GetBySystemName(Config["awsRegion"]));

                    var invReq = new CreateInvalidationRequest
                    {
                        DistributionId = distId,
                        InvalidationBatch = new InvalidationBatch
                        {
                            Paths = new Paths
                            {
                                Quantity = invalidPaths.Count,
                                Items = invalidPaths
                            },
                            CallerReference = DateTime.Now.Ticks.ToString()
                        }
                    };

                    var resp = client.CreateInvalidation(invReq);
                    if(resp.HttpStatusCode == HttpStatusCode.Created)
                    {
                        Log.Info(string.Format("Created invalidation for Cloudfront Distribution {0}", distId));
                        Log.Debug("Invalidated Paths", invalidPaths);
                    }
                    else
                    {
                        Log.Error(string.Format("Got unexpected result creating invalidation for Cloudfront Distribution {0}: {1}", distId, resp.HttpStatusCode));
                    }
                }
            }
        }
    }
}
