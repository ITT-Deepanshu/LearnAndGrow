using FinanceTracker.ConsoleClient.DTOs;
using FinanceTracker.ConsoleClient.Services;

namespace FinanceTracker.ConsoleClient.UI
{
    public class UserScreen
    {
        private readonly ApiService _api;

        public UserScreen(ApiService api)
        {
            _api = api;
        }

        public async Task<Guid> SelectOrCreateUser()
        {
            Console.WriteLine("1. Create User 2. Select Existing");
            var input = Console.ReadLine();

            if (input == "1")
            {
                Console.Write("Enter Name: ");
                var name = Console.ReadLine();

                await _api.PostAsync("users", new UserDto { Name = name });

                Console.WriteLine("User created.");
            }

            var users = await _api.GetAsync<List<UserResponseDto>>("users");

            if (users.Count == 0)
            {
                Console.WriteLine("No users found. Please create a user first.");
                return await SelectOrCreateUser();
            }

            Console.WriteLine("\nAvailable Users:");
            for (int i = 0; i < users.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {users[i].Name} ({users[i].Id})");
            }

            Console.Write("Select user number: ");
            var selection = int.Parse(Console.ReadLine());

            return users[selection - 1].Id;
        }
    }
}