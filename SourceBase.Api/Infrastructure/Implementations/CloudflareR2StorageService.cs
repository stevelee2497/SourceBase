using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Infrastructure.Implementations;

public class CloudflareR2StorageService(AppSettings appSettings) : IStorageService
{
    private AmazonS3Client CreateClient()
    {
        var r2 = appSettings.R2;
        var credentials = new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        };
        return new AmazonS3Client(credentials, config);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string objectKey, string contentType, int expiryMinutes = 15)
    {
        using var client = CreateClient();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = appSettings.R2.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            ContentType = contentType,
        };
        return await client.GetPreSignedURLAsync(request);
    }
}
