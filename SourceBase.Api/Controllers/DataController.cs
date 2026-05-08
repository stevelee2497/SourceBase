using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Application.Common;
using SourceBase.Application.Features.Data;
using SourceBase.Domain.Entities;

namespace SourceBase.Api.Controllers;

[ApiController]
[Route("api")]
public class DataController(ISender sender) : ControllerBase
{
    [HttpGet("audits")]
    [Authorize(Roles = Roles.Admin)]
    public Task<List<AuditHistoryEntity>> GetAudits([FromQuery] GetAuditsQuery query)
    {
        return sender.Send(query);
    }

    [HttpGet("roles")]
    public Task<List<RoleResponse>> GetRoles([FromQuery] GetRolesQuery query)
    {
        return sender.Send(query);
    }
}
