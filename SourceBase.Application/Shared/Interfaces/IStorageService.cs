namespace SourceBase.Application.Shared.Interfaces;

public interface IStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(string objectKey, string contentType);
}
