using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SourceBase.Api.Entities;
using SourceBase.Api.Features.Roles;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public record GetEnumsRequest(AvailableEnums[] Enums);

public record GetEnumsResponse(Dictionary<AvailableEnums, List<EnumResponse>> Data);

public record EnumResponse(string Name, string? Description);

public class GetEnumsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/data/enums", ([FromBody] GetEnumsRequest request, GetEnumsHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Data");
}

public class GetEnumsHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<GetEnumsRequest, GetEnumsResponse>
{
    private Dictionary<AvailableEnums, Task<List<EnumResponse>>> EnumTypeMapping => new()
    {
        { AvailableEnums.RolesOrder, BuildEnumDefinitions<RolesOrder>() },
        { AvailableEnums.TodoItemStatus, BuildEnumDefinitions<TodoItemStatus>() },
        { AvailableEnums.Roles, GetRolesAsync() },
    };

    public async Task<GetEnumsResponse> Handle(GetEnumsRequest request, CancellationToken ct)
    {
        if (request.Enums.Any(enumType => !EnumTypeMapping.ContainsKey(enumType)))
            throw new BadRequestException("One or more enum types are not supported");

        var enumDefinitions = await Task.WhenAll(request.Enums.Select(enumType => EnumTypeMapping[enumType]));
        var res = request.Enums.Zip(enumDefinitions, (key, value) => new { key, value }).ToDictionary(x => x.key, x => x.value);
        return new GetEnumsResponse(res);
    }

    private Task<List<EnumResponse>> GetRolesAsync()
    {
        return roleManager.Roles
            .Where(x => x.Name != null)
            .OrderBy(x => x.Name)
            .Select(x => new EnumResponse(x.Name!, x.Description))
            .ToListAsync();
    }

    private Task<List<EnumResponse>> BuildEnumDefinitions<TEnum>() where TEnum : struct, Enum
    {
        var results = Enum.GetValues<TEnum>()
            .Select(enumValue => new EnumResponse(enumValue.ToString(), enumValue.GetDisplayName() ?? enumValue.ToString()))
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