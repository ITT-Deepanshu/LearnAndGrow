using FluentValidation;
using PRM.Application.Features.SystemConfig.Commands;
using PRM.Domain.Enums;

namespace PRM.Application.Features.SystemConfig.Validators;

public sealed class UpdateLlmApiKeyCommandValidator : AbstractValidator<UpdateLlmApiKeyCommand>
{
    public UpdateLlmApiKeyCommandValidator()
    {
        RuleFor(x => x.ApiKey).NotEmpty();
    }
}

public sealed class UpdateLlmProviderCommandValidator : AbstractValidator<UpdateLlmProviderCommand>
{
    public UpdateLlmProviderCommandValidator()
    {
        RuleFor(x => x.Provider).IsInEnum().Must(p => p is AiProviderType.Gemini or AiProviderType.Grok);
    }
}

public sealed class UpdateSchedulerIntervalCommandValidator : AbstractValidator<UpdateSchedulerIntervalCommand>
{
    public UpdateSchedulerIntervalCommandValidator()
    {
        RuleFor(x => x.Minutes).GreaterThan(0);
    }
}

public sealed class UpdateMaxWeeklyHoursCommandValidator : AbstractValidator<UpdateMaxWeeklyHoursCommand>
{
    public UpdateMaxWeeklyHoursCommandValidator()
    {
        RuleFor(x => x.Hours).InclusiveBetween(1, 168);
    }
}
