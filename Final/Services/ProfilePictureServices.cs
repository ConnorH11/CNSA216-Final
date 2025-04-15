using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Threading.Tasks;

namespace ProfilePictureServices.Services
{
    public class ProfilePictureService
    {
        private readonly IConfiguration _configuration;

        public ProfilePictureService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetProfilePictureUrlAsync(string username)
        {
            string endpoint = _configuration["Minio:Endpoint"];
            int port = int.Parse(_configuration["Minio:Port"]);
            string accessKey = _configuration["Minio:AccessKey"];
            string secretKey = _configuration["Minio:SecretKey"];
            string bucketName = _configuration["Minio:BucketName"];
            string objectName = $"{username}_profile";

            var minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithSSL()
                .WithCredentials(accessKey, secretKey)
                .Build();

            string presignedUrl = null;
            try
            {
                presignedUrl = await minioClient.PresignedGetObjectAsync(
                    new PresignedGetObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithExpiry(24 * 60 * 60)
                );
            }
            catch (Exception ex)
            {
                presignedUrl = null;
            }

            return presignedUrl;
        }

        public async Task UploadProfilePictureAsync(string username, Microsoft.AspNetCore.Http.IFormFile profilePicture)
        {
            string endpoint = _configuration["Minio:Endpoint"];
            int port = int.Parse(_configuration["Minio:Port"]);
            string accessKey = _configuration["Minio:AccessKey"];
            string secretKey = _configuration["Minio:SecretKey"];
            string bucketName = _configuration["Minio:BucketName"];
            string objectName = $"{username}_profile";

            var minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithSSL()
                .WithCredentials(accessKey, secretKey)
                .Build();

            bool bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!bucketExists)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }

            using (var stream = profilePicture.OpenReadStream())
            {
                await minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(profilePicture.Length)
                    .WithContentType(profilePicture.ContentType)
                );
            }
        }
    }
}