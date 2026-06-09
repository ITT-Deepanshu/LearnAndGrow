using MediatR;
using PRM.Application.Features.Employees.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Queries;

public sealed class GetEmployeeByIdQueryHandler(
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IRequestHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
{
    public async Task<EmployeeDetailDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var employee = await employeeRepository.GetByIdWithSkillsAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        EnsureCanViewEmployee(employee.ManagerId);

        return EmployeeMappings.ToDetailDto(employee);
    }

    private void EnsureCanViewEmployee(long? managerId)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        if (currentUser.Role == UserRole.Manager && managerId == currentUser.UserId)
            return;

        throw new ForbiddenException("You do not have access to this employee.");
    }
}
