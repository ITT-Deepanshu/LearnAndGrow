using System.Linq.Expressions;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.SharedKernel;

namespace PRM.Domain.Specifications;

public sealed class AvailableEmployeesSpecification : Specification<Employee>
{
    private readonly decimal _minFreePercentage;

    public AvailableEmployeesSpecification(decimal minFreePercentage = 0) =>
        _minFreePercentage = minFreePercentage;

    public override Expression<Func<Employee, bool>> ToExpression() =>
        e => e.Status != EmployeeStatus.Inactive;
}
