using System.Text.Json;
using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Services;

public sealed class TokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public TokenStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "PRM.ConsoleClient");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "tokens.json");
    }

    public StoredTokens Load()
    {
        if (!File.Exists(_filePath))
            return new StoredTokens(null, null, null);

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<StoredTokens>(json, JsonOptions) ?? new StoredTokens(null, null, null);
        }
        catch
        {
            return new StoredTokens(null, null, null);
        }
    }

    public void Save(string? accessToken, string? refreshToken, long? employeeId)
    {
        var tokens = new StoredTokens(accessToken, refreshToken, employeeId);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(tokens, JsonOptions));
    }

    public void Clear() => Save(null, null, null);
}
