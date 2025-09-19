using Domain.Constants;
using Domain.Contexts;
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
    public async Task<IEnumerable<AuditHistoryEntity>> GetAudits()
    {
        return await dbContext.AuditHistories.ToListAsync();
    }

    [HttpGet("roles")]
    public async Task<IEnumerable<RoleEntity>> GetRoles()
    {
        return await dbContext.Roles.ToListAsync();
    }
}
