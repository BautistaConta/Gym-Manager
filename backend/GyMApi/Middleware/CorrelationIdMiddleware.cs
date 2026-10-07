using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GymManager.API.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requested = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsSafe(requested) ? requested! : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("HTTP {Method} {Path} finalizó con {StatusCode} en {ElapsedMilliseconds} ms.",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
        }
    }

    private static bool IsSafe(string? value) =>
        value is { Length: > 0 and <= 64 } && CorrelationIdPattern().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CorrelationIdPattern();
}
