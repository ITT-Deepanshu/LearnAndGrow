using MediatR;
using PRM.Domain.Enums;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed record UpdateLlmProviderCommand(AiProviderType Provider) : IRequest;
