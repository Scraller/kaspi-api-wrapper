using System;
using System.Threading.Tasks;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Simulates human behavior for more realistic request patterns to avoid detection
/// </summary>
public class KaspiHumanSimulator
{
    private readonly Random _random = new();

    /// <summary>
    /// Simulates human-like timing between actions based on the given strategy
    /// </summary>
    public async Task SimulateHumanTiming(AntiDetectionStrategy strategy)
    {
        if (strategy.SimulateHumanTiming)
        {
            // Base delay with jitter
            var baseDelay = _random.Next(200, 800);
            var jitter = (int)(baseDelay * 0.3 * (_random.NextDouble() * 2 - 1));
            var totalDelay = Math.Max(50, baseDelay + jitter);

            // Occasional longer pause (10% chance)
            if (_random.NextDouble() < 0.1)
            {
                totalDelay += _random.Next(1000, 3000);
            }

            await Task.Delay(totalDelay);
        }
    }

    /// <summary>
    /// Simulates network bandwidth limitations for more realistic request patterns
    /// </summary>
    public async Task SimulateBandwidthDelay(long contentLength)
    {
        // Calculate bandwidth delay
        var delayMs = CalculateBandwidthDelay(contentLength);
        await Task.Delay(delayMs);
    }

    /// <summary>
    /// Calculates a realistic delay based on content size and simulated bandwidth
    /// </summary>
    public int CalculateBandwidthDelay(long contentLength)
    {
        // Simulate network congestion (10% chance)
        double bandwidthKbps;
        if (_random.NextDouble() < 0.1)
        {
            // Slow connection
            bandwidthKbps = _random.Next(100, 500);
        }
        else
        {
            // Normal connection
            bandwidthKbps = _random.Next(500, 2000);
        }

        // Calculate delay based on content length and bandwidth
        var delayMs = (int)(contentLength * 8 / bandwidthKbps);

        // Add jitter (±20%)
        var jitterFactor = 1.0 + (_random.NextDouble() * 0.4) - 0.2;
        delayMs = (int)(delayMs * jitterFactor);

        // Cap to reasonable range
        return Math.Min(2000, Math.Max(50, delayMs));
    }
}