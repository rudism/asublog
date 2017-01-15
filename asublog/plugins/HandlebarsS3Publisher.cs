namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using Amazon;
    using Amazon.CloudFront;
    using Amazon.CloudFront.Model;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Transfer;
    using HandlebarsDotNet;
    using Core;

    public class HandlebarsS3Publisher : PublishingPlugin
    {
        private static readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        private string _assetpath;
        private Func<object, string> _index;
        private Func<object, string> _post;
        private int _postsPerPage;
        private AmazonS3Client _client;
        private string _bucket;

        public HandlebarsS3Publisher() : base("handlebarsS3Publisher", "0.5") { }

        public override void Init()
        {
            _bucket = Config["bucket"];
            var theme = Config["theme"];
            _postsPerPage = int.Parse(Config["postsPerPage"]);

            if(string.IsNullOrEmpty(theme))
                throw new ArgumentException("No theme was specified");

            var themePath = Path.Combine(_basePath, theme);
            _assetpath = Path.Combine(themePath, "assets");

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

        private string SaveIndex(Post[] posts, int pageNum, int maxPage)
        {
            var fname = string.Format("index{0}.html", pageNum > 0 ? pageNum.ToString() : null);

            var content = _index(new
            {
                posts,
                page = new
                {
                    num = pageNum,
                    max = maxPage,
                    isFirst = pageNum == 0,
                    isLast = pageNum == maxPage
                }
            });

            Log.Debug(string.Format("Rendered index {0}", fname), content);
            return UploadContent(fname, content);
        }

        private string SavePost(Post post)
        {
            if(_post == null) return null;

            var fname = string.Format("posts/{0}.html", post.Id);
            var content = _post(post);

            Log.Debug(string.Format("Rendered post {0}", fname), content);
            return UploadContent(fname, content);
        }

        private string UploadContent(string fname, string content)
        {
            string md5;
            using(var md5er = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = md5er.ComputeHash(bytes);
                var sb = new StringBuilder();
                for(var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                md5 = sb.ToString();
            }

            var upload = true;
            try
            {
                var headReq = new GetObjectMetadataRequest
                {
                    BucketName = _bucket,
                    Key = fname
                };

                var head = _client.GetObjectMetadata(headReq);
                var etag = head.ETag.Trim(new[] {'"'});
                Log.Debug(string.Format("Content md5 {0}, S3 etag {1}", md5, etag));
                if(md5.Equals(etag, StringComparison.OrdinalIgnoreCase))
                {
                    upload = false;
                    Log.Info(string.Format("Skipping s3://{0}/{1} (file unchanged)", _bucket, fname));
                }
            }
            catch(AmazonS3Exception ex)
            {
                if(ex.StatusCode != HttpStatusCode.NotFound) throw;
            }

            if(upload)
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
            }

            return upload ? string.Format("/{0}", fname) : null;
        }

        private List<string> UploadAssets()
        {
            var paths = 
            var assetpath = Path.Combine(
            Log.Debug("Uploading assets directory");
            var tu = new TransferUtility(_client);
            var req = new TransferUtilityUploadDirectoryRequest
            {

            };
            tu.UploadDirectory(req);
        }

        public override void Publish(IEnumerator<Post> posts, int count)
        {
            var invalidations = new List<string>();

            var maxPage = (int) Math.Ceiling((float) count / _postsPerPage) - 1;
            var page = 0;
            var pagePosts = new List<Post>();
            while(posts.MoveNext())
            {
                invalidations.Add(SavePost(posts.Current));
                pagePosts.Add(posts.Current);
                if(pagePosts.Count == _postsPerPage)
                {
                    invalidations.Add(SaveIndex(pagePosts.ToArray(), page++, maxPage));
                    pagePosts.Clear();
                }
            }
            if(pagePosts.Count > 0)
            {
                invalidations.Add(SaveIndex(pagePosts.ToArray(), page, maxPage));
            }

            var invalidPaths = invalidations.Where(i => !string.IsNullOrEmpty(i)).ToList();

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
                            Paths = new Paths { Quantity = 1, Items = invalidPaths },
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
