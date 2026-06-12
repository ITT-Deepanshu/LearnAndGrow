using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

public sealed class AiApiKeyResolver(
    ISystemConfigRepository systemConfigRepository,
    IApiKeyProtector apiKeyProtector)
{
    public async Task<string> ResolveAsync(string? providerApiKey, CancellationToken cancellationToken)
    {
        var key = await ResolveOptionalAsync(providerApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(key))
            throw new AiException("LLM API key is not configured. Set it in Admin → System Configuration.");

        return key;
    }

    public async Task<string?> ResolveOptionalAsync(string? providerApiKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(providerApiKey))
            return providerApiKey.Trim();

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.LlmApiKeyEncrypted))
            return null;

        return apiKeyProtector.Unprotect(config.LlmApiKeyEncrypted);
    }
}
