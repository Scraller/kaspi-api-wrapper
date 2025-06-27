using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Handles HTTP communication with Kaspi API including anti-detection mechanisms
/// </summary>
public class KaspiHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly HeaderGenerator _headerGenerator;
    private readonly RequestThrottler _requestThrottler;
    private readonly SessionManager _sessionManager;

    public KaspiHttpClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        HeaderGenerator headerGenerator,
        RequestThrottler requestThrottler,
        SessionManager sessionManager)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _headerGenerator = headerGenerator ?? throw new ArgumentNullException(nameof(headerGenerator));
        _requestThrottler = requestThrottler ?? throw new ArgumentNullException(nameof(requestThrottler));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <summary>
    /// Sends an HTTP request with anti-detection measures
    /// </summary>
    public async Task<(bool Success, string Content)> SendRequestAsync(
        string url,
        AntiDetectionStrategy strategy,
        string cityId,
        bool useSessionCookies = true,
        Dictionary<string, string>? customHeaders = null)
    {
        try
        {
            // Generate headers with anti-detection (excluding cookies for now)
            var tempStrategy = new AntiDetectionStrategy
            {
                RotateUserAgent = strategy.RotateUserAgent,
                RandomHeaders = strategy.RandomHeaders,
                SimulateBrowserFingerprint = strategy.SimulateBrowserFingerprint,
                AddNavigationTiming = strategy.AddNavigationTiming,
                AddReferrerPath = strategy.AddReferrerPath,
                VaryAcceptParams = strategy.VaryAcceptParams,
                SimulateMediaFeatures = strategy.SimulateMediaFeatures,
                AddSessionCookies = false  // We'll handle cookies separately
            };
            var headers = _headerGenerator.GenerateHeaders(tempStrategy, cityId);

            // Add cookies for session persistence using SessionManager for consistent city setting
            if (useSessionCookies)
            {
                headers["Cookie"] = _sessionManager.GenerateSessionCookies(cityId);
            }

            // Apply custom headers (e.g., for specific referrer)
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    headers[header.Key] = header.Value;
                }
            }

            // Apply anti-detection throttling
            await _requestThrottler.ThrottleRequest(url, strategy);

            // Create HTTP client with the headers
            var httpClient = GetHttpClient(headers);

            // Make HTTP request
            var response = await httpClient.GetAsync(url);

            // Simulate bandwidth throttling
            if (strategy.SimulateBandwidth)
            {
                await SimulateBandwidthDelay(response.Content.Headers.ContentLength ?? 0);
            }

            if (response.IsSuccessStatusCode)
            {
                // HttpClient with automatic decompression should handle this,
                // but let's read content as string which will auto-decompress
                string jsonContent = await response.Content.ReadAsStringAsync();

                // Log compression info for debugging
                var contentEncoding = response.Content.Headers.ContentEncoding;
                if (contentEncoding.Any())
                {
                    _logger.LogDebug("Response compressed with: {Encoding}", string.Join(", ", contentEncoding));
                }

                return (true, jsonContent);
            }
            else
            {
                _logger.LogWarning("HTTP {StatusCode} received from {Url}",
                    (int)response.StatusCode, url);
                return (false, string.Empty);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error for URL: {Url}", url);
            // For critical network errors, let them bubble up
            // For less critical HTTP errors, we handle them gracefully
            if (ex.Message.Contains("Network") || ex.Message.Contains("timeout") || ex.Message.Contains("connection"))
            {
                throw;
            }
            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request to {Url}", url);
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Sends a POST HTTP request with anti-detection measures
    /// </summary>
    public async Task<(bool Success, string Content)> SendPostRequestAsync(
        string url,
        HttpContent content,
        AntiDetectionStrategy strategy,
        string cityId,
        bool useSessionCookies = true,
        Dictionary<string, string>? customHeaders = null)
    {
        try
        {
            // Use the existing throttling mechanism
            await Task.Delay(100); // Simple throttling

            using var client = _httpClientFactory.CreateClient("KaspiScraper");
            client.Timeout = TimeSpan.FromSeconds(30);

            // Apply headers from generator
            var headers = _headerGenerator.GenerateHeaders(strategy);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add custom headers if provided
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add session cookies if enabled
            if (useSessionCookies)
            {
                var sessionId = _sessionManager.GenerateSessionId();
                client.DefaultRequestHeaders.Add("Cookie", $"sessionId={sessionId}");
            }

            // Add Referer header for POST requests - remove existing first to avoid conflicts
            client.DefaultRequestHeaders.Remove("Referer");
            client.DefaultRequestHeaders.Add("Referer", $"https://kaspi.kz/shop/p/{ExtractProductIdFromUrl(url)}/");

            _logger.LogDebug("Sending POST request to {Url}", url);

            var response = await client.PostAsync(url, content);

            // Session management handled by existing logic

            // Simulate bandwidth throttling
            if (strategy.SimulateBandwidth)
            {
                await SimulateBandwidthDelay(response.Content.Headers.ContentLength ?? 0);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return (true, responseContent);
            }
            else
            {
                _logger.LogWarning("POST HTTP {StatusCode} received from {Url}",
                    response.StatusCode, url);
                return (false, string.Empty);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("POST Request timeout for {Url}", url);
            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST Request failed for {Url}", url);
            return (false, string.Empty);
        }
    }

    private string ExtractProductIdFromUrl(string url)
    {
        // Extract product ID from URLs like /yml/offer-view/offers/102650292
        var segments = url.Split('/');
        return segments.Length > 0 ? segments[segments.Length - 1] : string.Empty;
    }

    private HttpClient GetHttpClient(Dictionary<string, string> headers)
    {
        var httpClient = _httpClientFactory.CreateClient("KaspiScraper");

        // Clear any existing headers
        httpClient.DefaultRequestHeaders.Clear();

        // Add all headers
        foreach (var header in headers)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return httpClient;
    }

    private async Task SimulateBandwidthDelay(long contentLength)
    {
        // Delegate to the human simulator
        await Task.Delay(CalculateBandwidthDelay(contentLength));
    }

    private int CalculateBandwidthDelay(long contentLength)
    {
        // Simulate network congestion (10% chance)
        Random random = new Random();
        double bandwidthKbps;
        if (random.NextDouble() < 0.1)
        {
            // Slow connection
            bandwidthKbps = random.Next(100, 500);
        }
        else
        {
            // Normal connection
            bandwidthKbps = random.Next(500, 2000);
        }

        // Calculate delay based on content length and bandwidth
        var delayMs = (int)(contentLength * 8 / bandwidthKbps);

        // Add jitter (±20%)
        var jitterFactor = 1.0 + (random.NextDouble() * 0.4) - 0.2;
        delayMs = (int)(delayMs * jitterFactor);

        // Cap to reasonable range
        return Math.Min(2000, Math.Max(50, delayMs));
    }
}