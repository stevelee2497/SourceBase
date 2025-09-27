using Domain.Abstractions;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

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
