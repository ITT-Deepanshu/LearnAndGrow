using MediatR;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed record UpdateLlmApiKeyCommand(string ApiKey) : IRequest;
