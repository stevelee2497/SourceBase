using FluentValidation;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Icons;

public record UploadIconImageRequest(string FileName);

public record UploadIconImageResponse(string UploadUrl, string IconUrl, string ContentType);

public class UploadIconImageEndpoint : IEndpoint
{
    public const string Route = "icons/upload-image";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (UploadIconImageRequest request, UploadIconImageHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Icons");
}

public class UploadIconImageHandler(IStorageService storageService, AppSettings appSettings) : IRequestHandler<UploadIconImageRequest, UploadIconImageResponse>
{
    public static readonly Dictionary<string, string> ContentTypeMappings = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".svg", "image/svg+xml" },
    };

    public async Task<UploadIconImageResponse> Handle(UploadIconImageRequest request, CancellationToken ct)
    {
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var objectKey = $"icons/{Guid.NewGuid()}{ext}";
        var contentType = ContentTypeMappings[ext];
        var uploadUrl = await storageService.GeneratePresignedUploadUrlAsync(objectKey, contentType);
        var iconUrl = $"{appSettings.R2.PublicUrl.TrimEnd('/')}/{objectKey}";
        return new UploadIconImageResponse(uploadUrl, iconUrl, contentType);
    }
}

public class UploadIconImageRequestValidator : AbstractValidator<UploadIconImageRequest>
{
    public UploadIconImageRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => UploadIconImageHandler.ContentTypeMappings.ContainsKey(Path.GetExtension(name ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Only jpg, png, gif, webp, and svg image files are allowed.");
    }
}
