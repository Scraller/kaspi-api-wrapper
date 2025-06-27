using ProductSearchEngine.Scraper.Kaspi;

namespace ProductSearchEngine.Api.Middleware;

/// <summary>
/// Middleware to log API requests and detect potential anti-bot triggers
/// </summary>
public class ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger = logger;

    /// <summary>
    /// Processes HTTP requests and logs them with anti-bot detection
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var userAgent = request.Headers.UserAgent.ToString();
        var referer = request.Headers.Referer.ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        // Log request details for anti-bot analysis
        _logger.LogInformation("API Request: {Method} {Path} | UA: {UserAgent} | Referer: {Referer} | IP: {IP}",
            request.Method, request.Path, userAgent, referer, remoteIp);

        // Detect potential bot patterns
        var isSuspiciousBotRequest = DetectSuspiciousRequest(userAgent, referer, request.Path);
        if (isSuspiciousBotRequest)
        {
            _logger.LogWarning("Potentially suspicious request detected: {Method} {Path} | UA: {UserAgent}",
                request.Method, request.Path, userAgent);
        }

        await _next(context);
    }

    private static bool DetectSuspiciousRequest(string userAgent, string referer, string path)
    {
        // Common bot indicators
        var botUserAgents = new[] { "swagger", "postman", "insomnia", "curl", "wget", "bot", "crawler" };
        var isSwaggerRequest = botUserAgents.Any(bot => userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase));
        
        // Rapid requests to Kaspi endpoints without proper referer
        var isKaspiEndpoint = path.Contains("/api/categories/kaspi", StringComparison.OrdinalIgnoreCase);
        var hasNoReferer = string.IsNullOrEmpty(referer);
        
        return isSwaggerRequest || (isKaspiEndpoint && hasNoReferer);
    }
}
