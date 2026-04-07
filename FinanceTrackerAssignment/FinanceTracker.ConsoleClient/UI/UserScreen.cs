using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Interfaces;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class UserScreen
    {
        private readonly IApiService _api;

        public UserScreen(IApiService api)
        {
            _api = api;
        }

        public async Task<Guid> SelectOrCreateUser()
        {
            while (true)
            {
                Console.WriteLine("\n1. Create User  2. Select Existing");
                var input = Console.ReadLine();

                if (input == "1")
                {
                    await CreateUsersFlow();
                }

                var users = await _api.GetAsync<List<UserResponseDto>>("users");

                if (users.Count == 0)
                {
                    Console.WriteLine("No users found. Please create a user first.");
                    continue;
                }

                Console.WriteLine("\nAvailable Users:");
                for (int i = 0; i < users.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {users[i].Name} ({users[i].Id})");
                }

                Console.Write("Select user number: ");
                if (!int.TryParse(Console.ReadLine(), out var selection)
                    || selection < 1
                    || selection > users.Count)
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                    continue;
                }

                return users[selection - 1].Id;
            }
        }

        private async Task CreateUsersFlow()
        {
            while (true)
            {
                Console.Write("Enter Name: ");
                var name = Console.ReadLine() ?? string.Empty;

                await _api.PostAsync("users", new UserDto { Name = name });
                Console.WriteLine("User created.");

                Console.Write("Create another user? (y/n): ");
                var another = Console.ReadLine()?.Trim().ToLower();
                if (another != "y") break;
            }
        }
    }
}