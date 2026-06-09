using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class AuditLogRepository(PrmDbContext context) : IAuditLogRepository
{
    public void Add(AuditLog auditLog) => context.AuditLogs.Add(auditLog);
}
