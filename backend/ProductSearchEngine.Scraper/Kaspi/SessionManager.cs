using System;
using System.Security.Cryptography;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Manages session data and cookies for Kaspi API requests
/// </summary>
public class SessionManager
{
    private string? _sessionId;
    private string? _fingerprint;
    private int _visitCount;
    private readonly Random _random = new();

    /// <summary>
    /// Generates a new session ID for consistent tracking across requests
    /// </summary>
    /// <returns>The generated session ID</returns>
    public string GenerateSessionId()
    {
        _sessionId = Guid.NewGuid().ToString("N")[..16];
        _fingerprint = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        _visitCount = 1;

        return _sessionId;
    }

    /// <summary>
    /// Generates session cookies for authenticated requests
    /// </summary>
    /// <param name="cityId">The city ID to include in cookies</param>
    /// <returns>A cookie string for HTTP headers</returns>
    public string GenerateSessionCookies(string cityId)
    {
        _sessionId ??= GenerateSessionId();
        _visitCount++;

        var cookies = new[]
        {
            $"ks-sessid={_sessionId}",
            $"ks-fingerprint={_fingerprint}",
            $"ks-visit={_visitCount}",
            $"_ga=GA1.2.{_random.Next(1000000, 9999999)}.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            $"kaspi.storefront.cookie.city={cityId}",
            $"ks.tg=2",
            $"layout=d"
        };

        return string.Join("; ", cookies);
    }
}
