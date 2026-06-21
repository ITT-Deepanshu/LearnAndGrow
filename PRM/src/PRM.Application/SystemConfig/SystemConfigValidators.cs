using FluentValidation;
using PRM.Application.SystemConfig;
using PRM.Domain.Enums;

namespace PRM.Application.SystemConfig;

public sealed class UpdateSystemConfigDtoValidator : AbstractValidator<UpdateSystemConfigDto>
{
    public UpdateSystemConfigDtoValidator()
    {
        RuleFor(x => x.LlmProvider)
            .IsInEnum()
            .Must(p => p is AiProviderType.Gemma)
            .WithMessage("LLM provider must be Gemma.");

        RuleFor(x => x.LlmApiKey)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.LlmApiKey) && !x.LlmApiKey.All(c => c == '*'));

        RuleFor(x => x.SchedulerIntervalMinutes).GreaterThan(0);
        RuleFor(x => x.MaxWeeklyHours).InclusiveBetween(1, 168);
    }
}
