using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;

using PRM.ConsoleClient;

using PRM.ConsoleClient.Menus;

using PRM.ConsoleClient.Services;

using PRM.ConsoleClient.Views;



var host = Host.CreateDefaultBuilder(args)

    .ConfigureServices(services => services.AddConsoleClient())

    .Build();



var ui = host.Services.GetRequiredService<ConsoleUi>();

var loginView = host.Services.GetRequiredService<LoginView>();

var session = host.Services.GetRequiredService<SessionContext>();

var tokenStore = host.Services.GetRequiredService<TokenStore>();



Console.CancelKeyPress += (_, e) =>

{

    e.Cancel = true;

    session.Clear();

    tokenStore.Clear();

    Environment.Exit(0);

};



try

{

    while (true)

    {

        if (!await loginView.RunAsync())

            break;



        while (session.IsAuthenticated)

        {

            var role = session.Role?.ToUpperInvariant();

            switch (role)

            {

                case "ADMIN":

                    await host.Services.GetRequiredService<AdminMenu>().RunAsync();

                    break;

                case "MANAGER":

                    await host.Services.GetRequiredService<ManagerMenu>().RunAsync();

                    break;

                case "RESOURCE":

                    await host.Services.GetRequiredService<EmployeeMenu>().RunAsync();

                    break;

                default:

                    ui.WriteError($"Unsupported role: {session.Role}");

                    session.Clear();

                    tokenStore.Clear();

                    break;

            }

        }

    }



    ui.ClearScreen();

    Console.WriteLine("Thank you for using PRM. Goodbye!");

}

catch (Exception ex)

{

    ui.WriteError(ex.Message);

    ui.Pause();

}

