using System.Text.Json;
using GymManager.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GyMApi.Tests.Observability;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task Unexpected_exception_returns_safe_problem_without_stack_or_internal_message()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("database-password=private\ninternal stack"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext { TraceIdentifier = "corr-safe" };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("corr-safe", json.RootElement.GetProperty("correlationId").GetString());
        var body = json.RootElement.ToString();
        Assert.DoesNotContain("private", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }
}
