using F360.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F360.Api.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeySettings> apiKeySettings)
{
    private readonly ApiKeySettings _apiKeySettings = apiKeySettings.Value;
    private readonly IEnumerable<string> PublicEndpoints = ["/health", "/swagger"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (PublicEndpoints.Any(x => context.Request.Path.StartsWithSegments(x)))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid or missing API key\"}");
            return;
        }

        if (!_apiKeySettings.Key.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid or missing API key\"}");
            return;
        }

        await next(context);
    }
}
