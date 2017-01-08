namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using HandlebarsDotNet;
    using Core;

    public class HandlebarsS3Publisher : PublishingPlugin
    {
        private static readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        private Func<object, string> _index;
        private int _postsPerPage;
        private AmazonS3Client _client;

        public HandlebarsS3Publisher() : base("handlebarsS3Publisher", "0.5") { }

        public override void Init()
        {
            var index = Config["index"];
            _postsPerPage = int.Parse(Config["postsPerPage"]);

            if(!index.StartsWith("/")) index = Path.Combine(_basePath, index);
            if(!File.Exists(index)) throw new FileNotFoundException("Could not find index template", index);
            _index = Handlebars.Compile(File.ReadAllText(index));

            _client = new AmazonS3Client(Config["awsKey"], Config["awsSecret"], RegionEndpoint.GetBySystemName(Config["awsRegion"]));
        }

        private void SaveIndex(Post[] posts, int pageNum)
        {
            var fname = string.Format("index{0}.html", pageNum > 0 ? pageNum.ToString() : null);
            var content = _index(new {posts});
            Log.Debug(string.Format("Rendered file {0}", fname), content);

            var putReq = new PutObjectRequest
            {
                BucketName = Config["bucket"],
                Key = fname,
                ContentBody = content
            };

            Log.Info(string.Format("Uploading s3://{0}/{1}", Config["bucket"], fname));
            _client.PutObject(putReq);
        }

        public override void Publish(IEnumerator<Post> posts)
        {
            var page = 0;
            var pagePosts = new List<Post>();
            while(posts.MoveNext())
            {
                pagePosts.Add(posts.Current);
                if(pagePosts.Count == _postsPerPage)
                {
                    SaveIndex(pagePosts.ToArray(), page++);
                    pagePosts.Clear();
                }
            }
            if(pagePosts.Count > 0)
            {
                SaveIndex(pagePosts.ToArray(), page);
            }
        }
    }
}
