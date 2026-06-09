using System.Linq.Expressions;
using PRM.Domain.Entities;
using PRM.SharedKernel;

namespace PRM.Domain.Specifications;

public sealed class AllocationOverlapSpecification : Specification<Allocation>
{
    private readonly long _employeeId;
    private readonly DateOnly _from;
    private readonly DateOnly _to;

    public AllocationOverlapSpecification(long employeeId, DateOnly from, DateOnly to)
    {
        _employeeId = employeeId;
        _from = from;
        _to = to;
    }

    public override Expression<Func<Allocation, bool>> ToExpression() =>
        a => a.EmployeeId == _employeeId
             && a.EndedAt == null
             && a.FromDate <= _to
             && a.ToDate >= _from;
}
