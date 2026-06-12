using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using PRM.Api.Filters;
using PRM.Domain.Exceptions;

namespace PRM.UnitTests.Api.Filters;

public class ValidationFilterTests
{
    private sealed record SampleDto(string Name);

    private sealed class SampleDtoValidator : AbstractValidator<SampleDto>
    {
        public SampleDtoValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ThrowsValidationExceptionWhenInvalid()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IValidator<SampleDto>)).Returns(new SampleDtoValidator());
        var filter = new ValidationFilter(services);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["dto"] = new SampleDto("") },
            Substitute.For<Controller>());

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], Substitute.For<Controller>()));
        };

        var act = () => filter.OnActionExecutionAsync(executingContext, next);
        await act.Should().ThrowAsync<PRM.Domain.Exceptions.ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_SkipsPrimitiveArguments()
    {
        var services = Substitute.For<IServiceProvider>();
        var filter = new ValidationFilter(services);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["weekStart"] = new DateOnly(2026, 5, 12) },
            Substitute.For<Controller>());

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], Substitute.For<Controller>()));
        };

        await filter.OnActionExecutionAsync(executingContext, next);
        nextCalled.Should().BeTrue();
    }
}
