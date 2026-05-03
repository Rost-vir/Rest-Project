using AuctionSystem.API.Exceptions;
using System.Net;
using System.Text.Json;

namespace AuctionSystem.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException    nfe => (HttpStatusCode.NotFound,            nfe.Message),
            ConflictException     ce => (HttpStatusCode.Conflict,            ce.Message),
            ValidationException   ve => (HttpStatusCode.BadRequest,          ve.Message),
            BusinessRuleException be => (HttpStatusCode.UnprocessableEntity, be.Message),
            _                        => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;
        var body = new { statusCode = (int)statusCode, error = statusCode.ToString(), message };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
