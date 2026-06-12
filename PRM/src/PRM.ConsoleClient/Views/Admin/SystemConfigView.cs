using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class SystemConfigView(SystemConfigApi systemConfig, ConsoleUi ui)
{
    private const int GemmaProviderId = 1;

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("SYSTEM CONFIGURATION");

            try
            {
                var config = await systemConfig.GetAsync(ct);
                Console.WriteLine("Current Settings:");
                Console.WriteLine($"  LLM Provider        :  Gemma (in-house)");
                Console.WriteLine($"  LLM API Key         :  {config.LlmApiKeyMasked}");
                Console.WriteLine("  LLM Endpoint URL    :  (API appsettings: Ai:Gemma:BaseUrl + /api/generate)");
                Console.WriteLine("  LLM API Key Header  :  apikey (sent on each Gemma request)");
                Console.WriteLine($"  Scheduler Interval  :  {FormatInterval(config.SchedulerIntervalMinutes)}");
                Console.WriteLine($"  Max Weekly Hours    :  {config.MaxWeeklyHours}");
                ui.DrawDivider();
                Console.WriteLine("1. Update Gemma API Key");
                Console.WriteLine("2. Update Scheduler Interval");
                Console.WriteLine("3. Update Max Weekly Hours");
                Console.WriteLine("4. Back");
                Console.WriteLine();

                switch (ui.Prompt("Enter option"))
                {
                    case "1": await UpdateApiKeyAsync(config, ct); break;
                    case "2": await UpdateSchedulerAsync(config, ct); break;
                    case "3": await UpdateMaxHoursAsync(config, ct); break;
                    case "4": return;
                    default: ui.WriteError("Invalid option."); ui.Pause(); break;
                }
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); return; }
        }
    }

    private async Task UpdateApiKeyAsync(SystemConfig config, CancellationToken ct)
    {
        var key = ui.Prompt("New Gemma API Key", secret: true);
        try
        {
            await systemConfig.UpdateAsync(new UpdateSystemConfigRequest(
                GemmaProviderId, key, config.SchedulerIntervalMinutes, config.MaxWeeklyHours), ct);
            ui.WriteSuccess("Gemma API key updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task UpdateSchedulerAsync(SystemConfig config, CancellationToken ct)
    {
        var minutes = ui.PromptInt("Scheduler interval (minutes)", 1);
        try
        {
            await systemConfig.UpdateAsync(new UpdateSystemConfigRequest(
                GemmaProviderId, config.LlmApiKeyMasked, minutes, config.MaxWeeklyHours), ct);
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
            await systemConfig.UpdateAsync(new UpdateSystemConfigRequest(
                GemmaProviderId, config.LlmApiKeyMasked, config.SchedulerIntervalMinutes, hours), ct);
            ui.WriteSuccess("Max weekly hours updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private static string FormatInterval(int minutes) =>
        minutes % 60 == 0 ? $"{minutes / 60} hours" : $"{minutes} minutes";
}
