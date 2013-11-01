using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Kraken.Core
{
    // https://portal.aws.amazon.com/gp/aws/securityCredentials
    // https://portal.aws.amazon.com/gp/aws/securityCredentials? supposedly will expire soon.
    // https://console.aws.amazon.com/s3/home?region=us-west-2
    public class S3Store
    {
        const string AWS_ACCESS_KEY_ID = "awsAccessKeyID";
        const string AWS_SECRET_KEY_ID = "awsSecretKeyID";
        const string S3_URL = "s3.amazonaws.com";

        private string accessKeyID;
        private string secretKeyID;
        private AmazonS3Client client;

        public S3Store(string accessID, string secretID)
        {
            accessKeyID = accessID;
            secretKeyID = secretID;
            AmazonS3Config s3Config = new AmazonS3Config {
                ServiceURL = S3_URL,
                CommunicationProtocol = Amazon.S3.Model.Protocol.HTTP
            };
            client = new AmazonS3Client(accessKeyID, secretKeyID, s3Config);
        }

        public S3Store(NameValueCollection settings) 
        {
            if (settings[AWS_ACCESS_KEY_ID] == null) 
                throw new Exception("missing_aws_access_key_id");
            if (settings[AWS_SECRET_KEY_ID] == null)
                throw new Exception("missing_aws_secret_key_id");
            accessKeyID = settings[AWS_ACCESS_KEY_ID];
            secretKeyID = settings[AWS_SECRET_KEY_ID];
            AmazonS3Config s3Config = new AmazonS3Config {
                ServiceURL = S3_URL,
                CommunicationProtocol = Amazon.S3.Model.Protocol.HTTP
            };
            client = new AmazonS3Client(accessKeyID, secretKeyID, s3Config);
        }

        public void GetFile(string s3Path, string localPath)
        {
            using (Stream s3 = GetStream(s3Path))
            {
                using (FileStream fs = File.Open(localPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    s3.CopyTo(fs);
                }
            }
        }

        public Stream GetStream(string s3Path)
        {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = getBucketName(s3Path);
            request.Key = getS3Key(s3Path);
            GetObjectResponse res = client.GetObject(request);
            return res.ResponseStream;
        }

        public void SaveStream(Stream s, string s3Path)
        {
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = getBucketName(s3Path);
            request.Key = getS3Key(s3Path);
            request.InputStream = s;
            client.PutObject(request);
        }

        public void SaveFile(string fullPath, string s3Path) {
            using (FileStream fs = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                SaveStream(fs, s3Path);
            }
        }

        public void CopyFile(string fromS3Path, string toS3Path)
        {
            CopyObjectRequest request = new CopyObjectRequest();
            request.SourceBucket = getBucketName(fromS3Path);
            request.SourceKey = getS3Key(fromS3Path);
            request.DestinationBucket = getBucketName(toS3Path);
            request.DestinationKey = getS3Key(toS3Path);
            CopyObjectResponse response = client.CopyObject(request);
            Console.WriteLine(response.ResponseXml);
        }

        public void MovePath(string fromS3Path, string toS3Path) {
            CopyFile(fromS3Path, toS3Path);
            DeleteFile(fromS3Path);
        }

        public void DeleteFile(string s3Path)
        {
            DeleteObjectRequest request = new DeleteObjectRequest();
            request.BucketName = getBucketName(s3Path);
            request.Key = getS3Key(s3Path);
            DeleteObjectResponse response = client.DeleteObject(request);
            Console.WriteLine(response.ResponseXml);
        }

        string getBucketName(string path)
        {
            // the idea is to get the data before the first slash.
            string[] segments = path.Split(new char[]{'/'});
            foreach (string segment in segments)
            {
                if (segment != "") 
                    return segment;
            }
            throw new Exception("s3path_empty");
        }

        string getS3Key(string path)
        {
            // remove beyond the first segment.
            List<string> segments = new List<string>(path.Split(new char[]{'/'}));
            int i = 0;
            bool first = false;
            while (i < segments.Count)
            {
                if (segments[i] != "") {
                    if (first) {
                        break;
                    } else {
                        first = true;
                        i++;
                        continue;
                    }
                } else {
                    i++;
                    continue;
                }
            }
            segments.RemoveRange(0, i);
            return string.Join("/", segments.ToArray());
        }
    }
}

