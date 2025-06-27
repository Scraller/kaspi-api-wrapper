using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductSearchEngine.Scraper.AntiDetection;

public class RequestThrottler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Dictionary<string, DateTime> _lastRequests = new();
    private readonly Random _random = new();
    private readonly object _lock = new();

    public RequestThrottler(int maxConcurrentRequests = 5)
    {
        _semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    public async Task ThrottleRequest(string endpoint, AntiDetectionStrategy strategy)
    {
        await _semaphore.WaitAsync();

        try
        {
            // Calculate delay
            var delay = CalculateDelay(endpoint, strategy);

            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private int CalculateDelay(string endpoint, AntiDetectionStrategy strategy)
    {
        lock (_lock)
        {
            // Base delay with jitter
            var baseDelay = strategy.BaseDelayMs;
            var jitter = (int)(baseDelay * strategy.JitterPercentage / 100 * (_random.NextDouble() * 2 - 1));
            var delay = Math.Max(50, baseDelay + jitter);

            // Human-like pauses
            if (_random.NextDouble() < strategy.RandomPauseProbability)
            {
                delay += _random.Next(1000, strategy.MaxPauseMs);
            }

            // Bandwidth simulation
            if (strategy.SimulateBandwidth)
            {
                delay += CalculateBandwidthDelay(strategy.RequestSizeKb);
            }

            // Ensure minimum time between requests to same endpoint
            if (_lastRequests.ContainsKey(endpoint))
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequests[endpoint];
                var minInterval = TimeSpan.FromMilliseconds(200);

                if (timeSinceLastRequest < minInterval)
                {
                    var additionalDelay = (int)(minInterval - timeSinceLastRequest).TotalMilliseconds;
                    delay += additionalDelay;
                }
            }

            _lastRequests[endpoint] = DateTime.UtcNow.AddMilliseconds(delay);
            return delay;
        }
    }

    private int CalculateBandwidthDelay(int requestSizeKb)
    {
        // Simulate realistic connection speeds
        var minSpeedKbps = 500;  // 500 kbps
        var maxSpeedKbps = 5000; // 5 Mbps

        // Simulate network congestion (10% chance)
        int bandwidthKbps;
        if (_random.NextDouble() < 0.1)
        {
            bandwidthKbps = (int)(minSpeedKbps + (maxSpeedKbps - minSpeedKbps) * 0.3);
        }
        else
        {
            bandwidthKbps = _random.Next(minSpeedKbps, maxSpeedKbps);
        }

        // Calculate delay: size(KB) / bandwidth(KB/s) * 1000 (to get ms)
        var delaySeconds = (double)requestSizeKb / (bandwidthKbps / 8.0);
        var delayMs = (int)(delaySeconds * 1000);

        // Add jitter
        var jitter = (int)(delayMs * (_random.NextDouble() * 0.2 - 0.1)); // ±10%

        return Math.Max(0, delayMs + jitter);
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
