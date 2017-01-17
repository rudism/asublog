namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using Amazon;
    using Amazon.CloudFront;
    using Amazon.CloudFront.Model;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Core;

    public class S3Util
    {
        private AmazonS3Client _s3client;
        private AmazonCloudFrontClient _cfclient;

        public ILogger Log { get; set; }

        public S3Util(string key, string secret, string region)
        {
            _s3client = new AmazonS3Client(key, secret,
                RegionEndpoint.GetBySystemName(region));

            _cfclient = new AmazonCloudFrontClient(key, secret,
                RegionEndpoint.GetBySystemName(region));
        }

        public string GetETag(string bucket, string key)
        {
            try
            {
                var headReq = new GetObjectMetadataRequest
                {
                    BucketName = bucket,
                    Key = key
                };

                var head = _s3client.GetObjectMetadata(headReq);
                return head.ETag.Trim(new[] {'"'});
            }
            catch(AmazonS3Exception ex)
            {
                if(ex.StatusCode != HttpStatusCode.NotFound) throw;
            }
            return null;
        }

        public bool UploadContent(string bucket, string key, string content)
        {
            string md5;
            using(var md5er = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = md5er.ComputeHash(bytes);
                md5 = Md5ToStr(hash);
            }

            var etag = GetETag(bucket, key);

            Log.Debug(string.Format("Content md5 {0}, S3 etag {1}", md5, etag));
            if(!md5.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                var putReq = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    ContentBody = content
                };

                var resp = _s3client.PutObject(putReq);
                if(resp.HttpStatusCode == HttpStatusCode.OK)
                {
                    Log.Info(string.Format("Uploaded s3://{0}/{1}", bucket, key));
                }
                else
                {
                    Log.Error(string.Format("Got unexpected result when uploading s3://{0}/{1}: {2}", bucket, key, resp.HttpStatusCode));
                }
                return true;
            }
            else
            {
                Log.Debug(string.Format("Skipping s3://{0}/{1} (file unchanged)", bucket, key));
            }
            return false;
        }

        public bool UploadFile(string bucket, string path, string key)
        {
            Log.Debug(string.Format("Uploading {0} to s3://{1}/{2}", path, bucket, key));
            string md5;
            using(var md5er = MD5.Create())
            {
                using(var stream = File.OpenRead(path))
                {
                    var hash = md5er.ComputeHash(stream);
                    md5 = Md5ToStr(hash);
                }
            }

            var etag = GetETag(bucket, key);

            if(!md5.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                var putReq = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    FilePath = path
                };

                var resp = _s3client.PutObject(putReq);
                if(resp.HttpStatusCode == HttpStatusCode.OK)
                {
                    Log.Info(string.Format("Uploaded s3://{0}/{1}", bucket, key));
                    return true;
                }
                else
                {
                    Log.Error(string.Format("Got unexpected result when uploading s3://{0}/{1}: {2}", bucket, key, resp.HttpStatusCode));
                }
            }
            else
            {
                Log.Debug(string.Format("Skipping s3://{0}/{1} (file unchanged)", bucket, key));
            }
            return false;
        }

        public string Md5ToStr(byte[] hash)
        {
            var sb = new StringBuilder();
            for(var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public void InvalidateCloudFrontDistribution(string distId, List<string> invalidPaths)
        {
            Log.Debug("Invalidating paths", invalidPaths);
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

            var resp = _cfclient.CreateInvalidation(invReq);
            if(resp.HttpStatusCode == HttpStatusCode.Created)
            {
                Log.Info(string.Format("Created invalidation for Cloudfront Distribution {0}", distId));
            }
            else
            {
                Log.Error(string.Format("Got unexpected result creating invalidation for Cloudfront Distribution {0}: {1}", distId, resp.HttpStatusCode));
            }
        }
    }
}
