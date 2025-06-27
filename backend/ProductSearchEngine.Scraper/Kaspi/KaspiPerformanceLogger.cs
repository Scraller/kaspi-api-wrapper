using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Handles performance logging and metrics for Kaspi API operations
/// </summary>
public class KaspiPerformanceLogger
{
    private readonly ILogger _logger;

    public KaspiPerformanceLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs performance metrics in a structured format
    /// </summary>
    public void LogPerformanceMetrics(long totalTimeMs, int productCount, Dictionary<string, long> metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[PERFORMANCE] Kaspi API scraping completed in {totalTimeMs}ms");
        sb.AppendLine($"[PERFORMANCE] Products found: {productCount}");
        sb.AppendLine("[PERFORMANCE] Detailed metrics:");

        foreach (var metric in metrics.OrderBy(m => m.Key))
        {
            sb.AppendLine($"  - {metric.Key}: {metric.Value}ms");
        }

        _logger.LogInformation(sb.ToString());
    }

    /// <summary>
    /// Records a timed operation and adds it to the metrics dictionary
    /// </summary>
    public long RecordOperation(Dictionary<string, long> metrics, string operationName, Action operation)
    {
        var startTime = DateTime.UtcNow.Ticks;
        operation();
        var endTime = DateTime.UtcNow.Ticks;

        var elapsedMs = (endTime - startTime) / TimeSpan.TicksPerMillisecond;
        metrics[operationName] = elapsedMs;
        return elapsedMs;
    }

    /// <summary>
    /// Records a timed async operation and adds it to the metrics dictionary
    /// </summary>
    public async Task<long> RecordOperationAsync(Dictionary<string, long> metrics, string operationName, Func<Task> operation)
    {
        var startTime = DateTime.UtcNow.Ticks;
        await operation();
        var endTime = DateTime.UtcNow.Ticks;

        var elapsedMs = (endTime - startTime) / TimeSpan.TicksPerMillisecond;
        metrics[operationName] = elapsedMs;
        return elapsedMs;
    }

    /// <summary>
    /// Records a timed async operation with result and adds it to the metrics dictionary
    /// </summary>
    public async Task<(T Result, long ElapsedMs)> RecordOperationWithResultAsync<T>(
        Dictionary<string, long> metrics,
        string operationName,
        Func<Task<T>> operation)
    {
        var startTime = DateTime.UtcNow.Ticks;
        var result = await operation();
        var endTime = DateTime.UtcNow.Ticks;

        var elapsedMs = (endTime - startTime) / TimeSpan.TicksPerMillisecond;
        metrics[operationName] = elapsedMs;
        return (result, elapsedMs);
    }
}
