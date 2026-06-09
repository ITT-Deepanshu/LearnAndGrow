using Microsoft.AspNetCore.DataProtection;
using PRM.Application.Interfaces.Security;

namespace PRM.Infrastructure.Security;

public sealed class ApiKeyProtector(IDataProtectionProvider provider) : IApiKeyProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("PRM.SystemConfig.LlmApiKey");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);

    public string Mask(string? protectedText) =>
        string.IsNullOrEmpty(protectedText) ? string.Empty : "****";
}
