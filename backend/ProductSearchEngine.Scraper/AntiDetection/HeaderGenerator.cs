using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ProductSearchEngine.Scraper.AntiDetection;

public class HeaderGenerator
{
    private readonly Random _random = new();

    private readonly string[] _userAgents = {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0"
    };

    private readonly string[] _acceptLanguages = {
        "en-US,en;q=0.9,ru;q=0.8,kk;q=0.7",
        "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,kk;q=0.6",
        "kk-KZ,kk;q=0.9,ru;q=0.8,en-US;q=0.7,en;q=0.6",
        "en-US,en;q=0.9,ru;q=0.8",
        "ru-RU,ru;q=0.9,en;q=0.8"
    };

    private readonly string[] _cacheControls = {
        "no-cache",
        "max-age=0",
        "no-store",
        "must-revalidate"
    };

    private readonly string[] _acceptEncodings = {
        "gzip, deflate, br",
        "gzip, deflate",
        "br",
        "gzip"
    };

    private readonly string[] _referrerPaths = {
        "/shop/c/smartphones/",
        "/shop/",
        "/shop/search/?text=smartphone",
        "/shop/c/mobile",
        "/shop/c/accessories",
        "/shop/cart",
        "/shop/favorites"
    };

    public Dictionary<string, string> GenerateHeaders(AntiDetectionStrategy strategy, string cityId = "551010000")
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["Accept-Language"] = _acceptLanguages[_random.Next(_acceptLanguages.Length)],
            ["Cache-Control"] = _cacheControls[_random.Next(_cacheControls.Length)],
            ["Accept-Encoding"] = _acceptEncodings[_random.Next(_acceptEncodings.Length)],
            ["Connection"] = "keep-alive",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "same-origin"
        };

        // Always rotate user agent
        if (strategy.RotateUserAgent)
        {
            headers["User-Agent"] = _userAgents[_random.Next(_userAgents.Length)];
        }

        // Add session cookies
        if (strategy.AddSessionCookies)
        {
            headers["Cookie"] = GenerateSessionCookies(cityId);
        }

        // Simulate browser fingerprinting
        if (strategy.SimulateBrowserFingerprint)
        {
            AddBrowserFingerprintHeaders(headers);
        }

        // Add navigation timing headers
        if (strategy.AddNavigationTiming)
        {
            AddNavigationTimingHeaders(headers);
        }

        // Add referrer path
        if (strategy.AddReferrerPath)
        {
            headers["Referer"] = $"https://kaspi.kz{_referrerPaths[_random.Next(_referrerPaths.Length)]}";
        }
        else
        {
            headers["Referer"] = "https://kaspi.kz/shop/";
        }

        // Vary accept parameters
        if (strategy.VaryAcceptParams)
        {
            VaryAcceptParameters(headers);
        }

        // Simulate media features
        if (strategy.SimulateMediaFeatures)
        {
            AddMediaFeatureHeaders(headers);
        }

        // Add random headers
        if (strategy.RandomHeaders)
        {
            AddRandomHeaders(headers);
        }

        return headers;
    }

    private string GenerateSessionCookies(string cityId)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..16];
        var fingerprint = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLower();
        var visitCount = _random.Next(1, 20);
        var gaId = _random.Next(1000000, 9999999);
        var gaTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _random.Next(1000000, 5000000);
        var ymUid = _random.NextInt64(1000000000L, 9999999999L);

        var cookies = new[]
        {
            $"ks-sessid={sessionId}",
            $"ks-fingerprint={fingerprint}",
            $"ks-visit={visitCount}",
            $"_ga=GA1.2.{gaId}.{gaTimestamp}",
            $"_ym_uid={ymUid}",
            $"kaspi.storefront.cookie.city={cityId}",
            $"ks.tg=2",
            $"layout=d"
        };

        return string.Join("; ", cookies);
    }

    private void AddBrowserFingerprintHeaders(Dictionary<string, string> headers)
    {
        var chromeVersion = _random.Next(118, 122);
        var platforms = new[] { "Windows", "macOS", "Linux" };
        var architectures = new[] { "x86", "arm" };
        var mobile = _random.Next(2) == 0 ? "?0" : "?1";

        headers["Sec-CH-UA"] = $"\"Chromium\";v=\"{chromeVersion}\", \"Google Chrome\";v=\"{chromeVersion}\", \"Not?A_Brand\";v=\"24\"";
        headers["Sec-CH-UA-Mobile"] = mobile;
        headers["Sec-CH-UA-Platform"] = $"\"{platforms[_random.Next(platforms.Length)]}\"";
        headers["Sec-CH-UA-Arch"] = $"\"{architectures[_random.Next(architectures.Length)]}\"";
        headers["Sec-CH-UA-Full-Version"] = $"\"{chromeVersion}.0.{_random.Next(1000, 9999)}.{_random.Next(10, 99)}\"";
    }

    private void AddNavigationTimingHeaders(Dictionary<string, string> headers)
    {
        var viewportWidths = new[] { 375, 390, 414, 768, 1024, 1280, 1366, 1440, 1920 };
        var viewportHeights = new[] { 667, 736, 812, 844, 896, 926, 1024, 1080, 1440 };

        headers["Sec-CH-Viewport-Width"] = viewportWidths[_random.Next(viewportWidths.Length)].ToString();
        headers["Sec-CH-Viewport-Height"] = viewportHeights[_random.Next(viewportHeights.Length)].ToString();
        headers["Sec-CH-UA-Platform-Version"] = $"\"{_random.Next(10, 16)}.{_random.Next(0, 10)}\"";
    }

    private void VaryAcceptParameters(Dictionary<string, string> headers)
    {
        var acceptValues = new List<string> { "application/json" };
        var possibleAdditions = new[]
        {
            "text/javascript", "application/javascript", "*/*",
            "text/html", "application/xhtml+xml"
        };

        var additionCount = _random.Next(1, 4);
        for (int i = 0; i < additionCount; i++)
        {
            var addition = possibleAdditions[_random.Next(possibleAdditions.Length)];
            var quality = 1.0 - (i * 0.1);
            acceptValues.Add($"{addition};q={quality:F1}");
        }

        headers["Accept"] = string.Join(", ", acceptValues);
    }

    private void AddMediaFeatureHeaders(Dictionary<string, string> headers)
    {
        // Device memory (30% chance)
        if (_random.NextDouble() < 0.3)
        {
            var deviceMemories = new[] { "0.25", "0.5", "1", "2", "4", "8", "16" };
            headers["Device-Memory"] = deviceMemories[_random.Next(deviceMemories.Length)];
        }

        // Connection type simulation (30% chance)
        if (_random.NextDouble() < 0.3)
        {
            headers["Downlink"] = _random.Next(1, 16).ToString(); // Mbps
            headers["ECT"] = new[] { "4g", "3g", "2g", "slow-2g" }[_random.Next(4)];
            headers["RTT"] = _random.Next(50, 501).ToString(); // Round trip time in ms
        }
    }

    private void AddRandomHeaders(Dictionary<string, string> headers)
    {
        var randomHeaders = new Dictionary<string, string[]>
        {
            ["DNT"] = new[] { "1", "0" },
            ["Upgrade-Insecure-Requests"] = new[] { "1" },
            ["TE"] = new[] { "trailers" },
            ["X-Requested-With"] = new[] { "XMLHttpRequest" },
            ["Save-Data"] = new[] { "on", "" },
            ["Sec-Fetch-User"] = new[] { "?1" },
            ["Pragma"] = new[] { "no-cache" }
        };

        // Add 1-4 random headers
        var headerCount = _random.Next(1, 5);
        var availableHeaders = randomHeaders.Keys.ToArray();

        for (int i = 0; i < headerCount; i++)
        {
            var headerKey = availableHeaders[_random.Next(availableHeaders.Length)];
            if (!headers.ContainsKey(headerKey))
            {
                var values = randomHeaders[headerKey];
                headers[headerKey] = values[_random.Next(values.Length)];
            }
        }
    }
}
