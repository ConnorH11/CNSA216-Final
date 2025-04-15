using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using System;
using System.Threading.Tasks;

namespace Final.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProfileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get the current user's username
            string username = User.Identity.Name;

            // Read MinIO settings from configuration
            string endpoint = _configuration["Minio:Endpoint"];       
            int port = int.Parse(_configuration["Minio:Port"]);         
            string accessKey = _configuration["Minio:AccessKey"];
            string secretKey = _configuration["Minio:SecretKey"];
            string bucketName = _configuration["Minio:BucketName"];     

            // Define the object name for the profile picture
            string objectName = $"{username}_profile"; 

            // Build the MinIO client
            var minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithSSL() 
                .WithCredentials(accessKey, secretKey)
                .Build();

            // Generate a presigned URL valid for 1 day 
            string presignedUrl = null;
            try
            {
                presignedUrl = await minioClient.PresignedGetObjectAsync(
                    new PresignedGetObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithExpiry(24 * 60 * 60)  // 86400 seconds 
                );
            }
            catch (Exception ex)
            {
                presignedUrl = null;
            }

            // Pass the username and presigned URL to the view.
            ViewBag.Username = username;
            ViewBag.ProfilePictureUrl = presignedUrl;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(Microsoft.AspNetCore.Http.IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                string username = User.Identity.Name;
                string endpoint = _configuration["Minio:Endpoint"];
                int port = int.Parse(_configuration["Minio:Port"]);
                string accessKey = _configuration["Minio:AccessKey"];
                string secretKey = _configuration["Minio:SecretKey"];
                string bucketName = _configuration["Minio:BucketName"];

                var minioClient = new MinioClient()
                    .WithEndpoint(endpoint, port)
                    .WithSSL()
                    .WithCredentials(accessKey, secretKey)
                    .Build();

                // Ensure the bucket exists
                bool bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!bucketExists)
                {
                    await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                }

                // Use a fixed naming convention for the object
                string objectName = $"{username}_profile";

                // Upload the file
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

            return RedirectToAction("Index");
        }
    }
}
