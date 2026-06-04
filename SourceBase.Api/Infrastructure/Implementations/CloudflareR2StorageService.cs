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
        var credentials = new BasicAWSCredentials(appSettings.R2.AccessKeyId, appSettings.R2.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = appSettings.R2.ServiceURL,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        };
        return new AmazonS3Client(credentials, config);
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string objectKey, string contentType)
    {
        using var client = CreateClient();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = appSettings.R2.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(appSettings.R2.ExpiryMinutes),
            ContentType = contentType,
        };
        return await client.GetPreSignedURLAsync(request);
    }
}
