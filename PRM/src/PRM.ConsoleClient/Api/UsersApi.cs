using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class UsersApi(PrmHttpClient http)
{
    public Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default) =>
        http.PostAsync<CreateUserRequest, UserResponse>("api/v1/users", request, ct);

    public Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<UserListItem>>("api/v1/users", ct);

    public Task<UserResponse> GetAsync(long id, CancellationToken ct = default) =>
        http.GetAsync<UserResponse>($"api/v1/users/{id}", ct);

    public Task ResetPasswordAsync(long id, string newPassword, string confirmPassword, CancellationToken ct = default) =>
        http.PostAsync($"api/v1/users/{id}/reset-password", new ResetPasswordRequest(newPassword, confirmPassword), ct);

    public Task DeactivateAsync(long id, CancellationToken ct = default) =>
        http.PostAsync($"api/v1/users/{id}/deactivate", new { }, ct);

    public Task ReactivateAsync(long id, CancellationToken ct = default) =>
        http.PostAsync($"api/v1/users/{id}/reactivate", new { }, ct);
}
