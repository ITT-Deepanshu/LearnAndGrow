using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class SystemConfigView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("SYSTEM CONFIGURATION");

            try
            {
                var config = await api.GetSystemConfigAsync(ct);
                Console.WriteLine("Current Settings:");
                Console.WriteLine($"  LLM Provider        :  {config.LlmProvider}");
                Console.WriteLine($"  LLM API Key         :  {config.LlmApiKeyMasked}");
                Console.WriteLine($"  Scheduler Interval  :  {FormatInterval(config.SchedulerIntervalMinutes)}");
                Console.WriteLine($"  Max Weekly Hours    :  {config.MaxWeeklyHours}");
                ui.DrawDivider();
                Console.WriteLine("1. Update LLM API Key");
                Console.WriteLine("2. Change LLM Provider  (Gemini / Groq)");
                Console.WriteLine("3. Update Scheduler Interval");
                Console.WriteLine("4. Update Max Weekly Hours");
                Console.WriteLine("5. Back");
                Console.WriteLine();

                switch (ui.Prompt("Enter option"))
                {
                    case "1": await UpdateApiKeyAsync(config, ct); break;
                    case "2": await UpdateProviderAsync(config, ct); break;
                    case "3": await UpdateSchedulerAsync(config, ct); break;
                    case "4": await UpdateMaxHoursAsync(config, ct); break;
                    case "5": return;
                    default: ui.WriteError("Invalid option."); ui.Pause(); break;
                }
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); return; }
        }
    }

    private async Task UpdateApiKeyAsync(SystemConfig config, CancellationToken ct)
    {
        var key = ui.Prompt("New LLM API Key", secret: true);
        try
        {
            await api.UpdateSystemConfigAsync(new UpdateSystemConfigRequest(
                ProviderToInt(config.LlmProvider), key, config.SchedulerIntervalMinutes, config.MaxWeeklyHours), ct);
            ui.WriteSuccess("LLM API key updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task UpdateProviderAsync(SystemConfig config, CancellationToken ct)
    {
        Console.WriteLine("Provider: (1) Gemini  (2) Groq");
        var provider = ui.PromptInt("Enter choice", 1, 2);
        try
        {
            await api.UpdateSystemConfigAsync(new UpdateSystemConfigRequest(
                provider, config.LlmApiKeyMasked, config.SchedulerIntervalMinutes, config.MaxWeeklyHours), ct);
            ui.WriteSuccess("LLM provider updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task UpdateSchedulerAsync(SystemConfig config, CancellationToken ct)
    {
        var minutes = ui.PromptInt("Scheduler interval (minutes)", 1);
        try
        {
            await api.UpdateSystemConfigAsync(new UpdateSystemConfigRequest(
                ProviderToInt(config.LlmProvider), config.LlmApiKeyMasked, minutes, config.MaxWeeklyHours), ct);
            ui.WriteSuccess("Scheduler interval updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task UpdateMaxHoursAsync(SystemConfig config, CancellationToken ct)
    {
        var hours = ui.PromptInt("Max weekly hours", 1, 168);
        try
        {
            await api.UpdateSystemConfigAsync(new UpdateSystemConfigRequest(
                ProviderToInt(config.LlmProvider), config.LlmApiKeyMasked, config.SchedulerIntervalMinutes, hours), ct);
            ui.WriteSuccess("Max weekly hours updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private static string FormatInterval(int minutes) =>
        minutes % 60 == 0 ? $"{minutes / 60} hours" : $"{minutes} minutes";

    private static int ProviderToInt(string provider) =>
        provider.Contains("Groq", StringComparison.OrdinalIgnoreCase) ||
        provider.Contains("Grok", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
}
