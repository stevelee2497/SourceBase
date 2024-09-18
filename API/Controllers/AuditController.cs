using Core.DbContexts;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/audits")]
    public class AuditController(IDbContext dbContext) : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<AuditHistoryEntity>> GetAudits()
        {
            return await dbContext.AuditHistories.ToListAsync();
        }
    }
}
