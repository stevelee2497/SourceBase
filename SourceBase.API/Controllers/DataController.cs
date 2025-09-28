using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;

namespace SourceBase.Api.Controllers;

[ApiController]
[Route("api")]
public class DataController(IDbContext dbContext) : ControllerBase
{
    [HttpGet("audits")]
    [Authorize(Roles = Roles.Admin)]
    public Task<List<AuditHistoryEntity>> GetAudits()
    {
        return dbContext.AuditHistories.ToListAsync();
    }

    [HttpGet("roles")]
    public Task<List<RoleEntity>> GetRoles()
    {
        return dbContext.Roles.ToListAsync();
    }
}
