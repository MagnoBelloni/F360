using F360.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace F360.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "Não foi possível completar sua solicitação";

        if (exception is BusinessException businessException)
        {
            statusCode = (HttpStatusCode)businessException.StatusCode;
            message = businessException.Message;

            logger.LogWarning(businessException, "Business exception occurred: {Message}", message);
        }
        else
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new { error = message });
        return context.Response.WriteAsync(result);
    }
}
