using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Abstractions;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Features.Data;

public record GetAuditsQuery() : IRequest<List<AuditHistoryEntity>>;

public class GetAuditsQueryHandler(IDbContext dbContext) : IRequestHandler<GetAuditsQuery, List<AuditHistoryEntity>>
{
    public Task<List<AuditHistoryEntity>> Handle(GetAuditsQuery request, CancellationToken cancellationToken)
    {
        return dbContext.AuditHistories.ToListAsync(cancellationToken);
    }
}
