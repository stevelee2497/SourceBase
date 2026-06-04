using FluentValidation;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Files;

public record GenerateAvatarUploadUrlRequest(string FileName);

public record GenerateAvatarUploadUrlResponse(string UploadUrl, string AvatarUrl, string ContentType);

public class GenerateAvatarUploadUrlEndpoint : IEndpoint
{
    public const string Route = "files/avatar/upload-url";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (GenerateAvatarUploadUrlRequest request, GenerateAvatarUploadUrlHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Files");
}

public class GenerateAvatarUploadUrlHandler(IStorageService storageService, AppSettings appSettings) : IRequestHandler<GenerateAvatarUploadUrlRequest, GenerateAvatarUploadUrlResponse>
{
    public static readonly Dictionary<string, string> ContentTypeMappings = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
    };

    public async Task<GenerateAvatarUploadUrlResponse> Handle(GenerateAvatarUploadUrlRequest request, CancellationToken ct)
    {
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var objectKey = $"avatars/{Guid.NewGuid()}{ext}";
        var contentType = ContentTypeMappings[ext];
        var uploadUrl = await storageService.GeneratePresignedUploadUrlAsync(objectKey, contentType);
        var avatarUrl = $"{appSettings.R2.PublicUrl.TrimEnd('/')}/{objectKey}";
        return new GenerateAvatarUploadUrlResponse(uploadUrl, avatarUrl, contentType);
    }
}

public class GenerateAvatarUploadUrlRequestValidator : AbstractValidator<GenerateAvatarUploadUrlRequest>
{
    public GenerateAvatarUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => GenerateAvatarUploadUrlHandler.ContentTypeMappings.ContainsKey(Path.GetExtension(name ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Only jpg, png, gif, and webp image files are allowed.");
    }
}
