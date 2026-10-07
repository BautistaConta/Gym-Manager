using GymManager.API.Services;

namespace GymManager.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, title, detail) = exception switch
            {
                DomainException => (StatusCodes.Status400BadRequest, "Solicitud inválida", exception.Message),
                TimeoutException => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible", "La base de datos no respondió a tiempo."),
                _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
            };
            if (status >= 500)
                logger.LogError(exception, "Error HTTP no controlado con estado {StatusCode}.", status);
            else
                logger.LogWarning("Solicitud rechazada con estado {StatusCode}: {ErrorType}.", status, exception.GetType().Name);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                detail,
                correlationId = context.TraceIdentifier
            });
        }
    }
}
