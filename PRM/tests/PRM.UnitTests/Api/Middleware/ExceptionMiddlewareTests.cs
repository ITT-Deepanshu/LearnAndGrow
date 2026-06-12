using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PRM.Api.Middleware;
using PRM.Domain.Exceptions;

namespace PRM.UnitTests.Api.Middleware;

public class ExceptionMiddlewareTests
{
    [Theory]
    [InlineData(typeof(ValidationException), 400)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(BusinessRuleException), 422)]
    [InlineData(typeof(ConflictException), 409)]
    public async Task InvokeAsync_MapsKnownExceptionsToStatusCodes(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "test message")!;
        var middleware = new ExceptionMiddleware(_ => throw exception, NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(expectedStatus);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_MapsUnknownExceptionTo500()
    {
        var middleware = new ExceptionMiddleware(_ => throw new InvalidOperationException("boom"), NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }
}
