using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
}
