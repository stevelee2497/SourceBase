using Domain.Constants;
using Domain.Contexts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/audits")]
public class AuditController(IDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IEnumerable<AuditHistoryEntity>> GetAudits()
    {
        return await dbContext.AuditHistories.ToListAsync();
    }
}
