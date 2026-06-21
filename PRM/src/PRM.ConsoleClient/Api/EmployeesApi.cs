using PRM.ConsoleClient.Models;

using PRM.ConsoleClient.Services;



namespace PRM.ConsoleClient.Api;



public sealed class EmployeesApi(PrmHttpClient http)

{

    public Task<IReadOnlyList<EmployeeListItem>> ListAsync(int? status = null, string? department = null, CancellationToken ct = default)

    {

        var query = PrmHttpClient.BuildQuery(("status", status?.ToString()), ("department", department));

        return http.GetAsync<IReadOnlyList<EmployeeListItem>>($"api/v1/employees{query}", ct);

    }



    public Task<EmployeeDetail> GetAsync(long id, CancellationToken ct = default) =>

        http.GetAsync<EmployeeDetail>($"api/v1/employees/{id}", ct);



    public Task UpdateAsync(long id, string department, string designation, CancellationToken ct = default) =>

        http.PutAsync($"api/v1/employees/{id}", new UpdateEmployeeRequest(department, designation), ct);



    public Task DeactivateAsync(long id, CancellationToken ct = default) =>

        http.PostAsync($"api/v1/employees/{id}/deactivate", new { }, ct);



    public Task ReactivateAsync(long id, CancellationToken ct = default) =>

        http.PostAsync($"api/v1/employees/{id}/reactivate", new { }, ct);



    public Task AssignManagerAsync(long employeeId, long managerUserId, CancellationToken ct = default) =>

        http.PostAsync($"api/v1/employees/{employeeId}/assign-manager", new AssignManagerRequest(managerUserId), ct);



    public Task<ResourceProfileSkill> AddSkillAsync(long employeeId, string name, int category, int proficiency, CancellationToken ct = default) =>

        http.PostAsync<AddSkillRequest, ResourceProfileSkill>($"api/v1/employees/{employeeId}/skills", new(name, category, proficiency), ct);



    public Task UpdateSkillProficiencyAsync(long employeeId, long skillId, int proficiency, CancellationToken ct = default) =>

        http.PutAsync($"api/v1/employees/{employeeId}/skills/{skillId}", new UpdateSkillProficiencyRequest(proficiency), ct);



    public Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken ct = default) =>

        http.DeleteAsync($"api/v1/employees/{employeeId}/skills/{skillId}", ct);

}


