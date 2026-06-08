using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Roles;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Application.Features.Data;

public record GetEnumsRequest(AvailableEnums[] Enums);

public record GetEnumsResponse(Dictionary<AvailableEnums, List<EnumResponse>> Data);

public record EnumResponse(string Name, string? Description);

public class GetEnumsEndpoint : IEndpoint
{
    public const string Route = "data/enums";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] GetEnumsRequest request, GetEnumsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Data");
}

public class GetEnumsHandler(IDbContext dbContext) : IRequestHandler<GetEnumsRequest, GetEnumsResponse>
{
    public async Task<GetEnumsResponse> Handle(GetEnumsRequest request, CancellationToken ct)
    {
        var enumDefinitions = await Task.WhenAll(request.Enums.Select(enumType => GetEnumDefinitionsAsync(enumType, ct)));
        var res = request.Enums.Zip(enumDefinitions, (key, value) => new { key, value }).ToDictionary(x => x.key, x => x.value);
        return new GetEnumsResponse(res);
    }

    private Task<List<EnumResponse>> GetEnumDefinitionsAsync(AvailableEnums enumType, CancellationToken ct)
    {
        return enumType switch
        {
            AvailableEnums.RolesOrder => BuildEnumDefinitions<RolesOrder>(),
            AvailableEnums.TodoItemStatus => BuildEnumDefinitions<TodoItemStatus>(),
            AvailableEnums.Roles => GetRolesAsync(ct),
            _ => throw new BadRequestException("One or more enum types are not supported")
        };
    }

    private Task<List<EnumResponse>> GetRolesAsync(CancellationToken ct)
    {
        return dbContext.Roles
            .Where(x => x.Name != null)
            .OrderBy(x => x.Name)
            .Select(x => new EnumResponse(x.Name!, x.Description))
            .ToListAsync(ct);
    }

    private Task<List<EnumResponse>> BuildEnumDefinitions<TEnum>() where TEnum : struct, Enum
    {
        var results = Enum.GetValues<TEnum>()
            .Select(enumValue => new EnumResponse(enumValue.ToString(), enumValue.ToString()))
            .ToList();
        return Task.FromResult(results);
    }
}

public class GetEnumsRequestValidator : AbstractValidator<GetEnumsRequest>
{
    public GetEnumsRequestValidator()
    {
        RuleFor(x => x.Enums).NotEmpty();
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AvailableEnums
{
    RolesOrder,
    TodoItemStatus,
    Roles,
}