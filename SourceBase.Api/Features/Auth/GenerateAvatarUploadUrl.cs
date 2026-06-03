using FluentValidation;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record GenerateAvatarUploadUrlRequest(string FileName);

public record GenerateAvatarUploadUrlResponse(string UploadUrl, string AvatarUrl, string ContentType);

public class GenerateAvatarUploadUrlEndpoint : IEndpoint
{
    public const string Route = "auth/avatar/upload-url";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (GenerateAvatarUploadUrlRequest request, GenerateAvatarUploadUrlHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Auth");
}

public class GenerateAvatarUploadUrlHandler(IStorageService storageService, AppSettings appSettings) : IRequestHandler<GenerateAvatarUploadUrlRequest, GenerateAvatarUploadUrlResponse>
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public async Task<GenerateAvatarUploadUrlResponse> Handle(GenerateAvatarUploadUrlRequest request, CancellationToken ct)
    {
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var objectKey = $"avatars/{Guid.NewGuid()}{ext}";
        var contentType = GetContentType(ext);
        var uploadUrl = await storageService.GeneratePresignedUploadUrlAsync(objectKey, contentType);
        var avatarUrl = $"{appSettings.R2.PublicUrl.TrimEnd('/')}/{objectKey}";
        return new GenerateAvatarUploadUrlResponse(uploadUrl, avatarUrl, contentType);
    }

    private static string GetContentType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => throw new BadRequestException("Unsupported file type."),
    };
}

public class GenerateAvatarUploadUrlRequestValidator : AbstractValidator<GenerateAvatarUploadUrlRequest>
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public GenerateAvatarUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Only jpg, png, gif, and webp image files are allowed.");
    }
}
