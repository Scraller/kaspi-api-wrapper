using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ProductSearchEngine.Scraper.AntiDetection;

public class AntiDetectionManager
{
    private readonly Random _random = new();
    private readonly string[] _availableStrategies = {
        "rotate_user_agent", "random_headers", "rotate_city_id",
        "vary_query_params", "add_session_cookies", "rotate_endpoints",
        "simulate_browser_fingerprint", "simulate_human_timing",
        "add_navigation_timing", "add_referrer_path", "cache_busting",
        "vary_accept_params", "simulate_media_features"
    };

    private readonly Dictionary<string, int> _strategySuccessCount = new();
    private readonly Dictionary<string, int> _strategyTotalCount = new();

    public AntiDetectionStrategy SelectStrategy()
    {
        // Start with proven strategies
        var selectedStrategies = new List<string> { "rotate_user_agent", "random_headers" };

        // Add successful strategies (70% chance)
        if (_random.NextDouble() < 0.7)
        {
            var successfulStrategy = GetBestPerformingStrategy();
            if (!string.IsNullOrEmpty(successfulStrategy) && !selectedStrategies.Contains(successfulStrategy))
            {
                selectedStrategies.Add(successfulStrategy);
            }
        }

        // Add random strategies
        var remainingStrategies = _availableStrategies.Except(selectedStrategies).ToArray();
        var strategyCount = _random.Next(2, 5); // 2-4 additional strategies

        for (int i = 0; i < strategyCount && remainingStrategies.Length > 0; i++)
        {
            var strategy = remainingStrategies[_random.Next(remainingStrategies.Length)];
            selectedStrategies.Add(strategy);
            remainingStrategies = remainingStrategies.Where(s => s != strategy).ToArray();
        }

        return BuildStrategyFromList(selectedStrategies);
    }

    public void RecordStrategyResult(AntiDetectionStrategy strategy, bool success)
    {
        var strategyKey = GetStrategyKey(strategy);

        if (!_strategyTotalCount.ContainsKey(strategyKey))
        {
            _strategyTotalCount[strategyKey] = 0;
            _strategySuccessCount[strategyKey] = 0;
        }

        _strategyTotalCount[strategyKey]++;
        if (success)
        {
            _strategySuccessCount[strategyKey]++;
        }
    }

    private string GetBestPerformingStrategy()
    {
        var bestStrategy = "";
        var bestSuccessRate = 0.0;

        foreach (var strategy in _availableStrategies)
        {
            if (_strategyTotalCount.ContainsKey(strategy) && _strategyTotalCount[strategy] >= 10)
            {
                var successRate = (double)_strategySuccessCount[strategy] / _strategyTotalCount[strategy];
                if (successRate > bestSuccessRate)
                {
                    bestSuccessRate = successRate;
                    bestStrategy = strategy;
                }
            }
        }

        return bestStrategy;
    }

    private AntiDetectionStrategy BuildStrategyFromList(List<string> strategies)
    {
        var strategy = new AntiDetectionStrategy();

        foreach (var strategyName in strategies)
        {
            switch (strategyName)
            {
                case "rotate_user_agent":
                    strategy.RotateUserAgent = true;
                    break;
                case "random_headers":
                    strategy.RandomHeaders = true;
                    break;
                case "rotate_city_id":
                    strategy.RotateCityId = true;
                    break;
                case "vary_query_params":
                    strategy.VaryQueryParams = true;
                    break;
                case "add_session_cookies":
                    strategy.AddSessionCookies = true;
                    break;
                case "simulate_browser_fingerprint":
                    strategy.SimulateBrowserFingerprint = true;
                    break;
                case "simulate_human_timing":
                    strategy.SimulateHumanTiming = true;
                    break;
                case "add_navigation_timing":
                    strategy.AddNavigationTiming = true;
                    break;
                case "add_referrer_path":
                    strategy.AddReferrerPath = true;
                    break;
                case "cache_busting":
                    strategy.CacheBusting = true;
                    break;
                case "vary_accept_params":
                    strategy.VaryAcceptParams = true;
                    break;
                case "simulate_media_features":
                    strategy.SimulateMediaFeatures = true;
                    break;
            }
        }

        // Set timing parameters with some randomization
        strategy.BaseDelayMs = _random.Next(250, 500);
        strategy.JitterPercentage = _random.Next(20, 40);
        strategy.RandomPauseProbability = _random.NextDouble() * 0.2; // 0-20%
        strategy.MaxPauseMs = _random.Next(3000, 8000);

        return strategy;
    }

    private string GetStrategyKey(AntiDetectionStrategy strategy)
    {
        var components = new List<string>();

        if (strategy.RotateUserAgent) components.Add("ua");
        if (strategy.RandomHeaders) components.Add("rh");
        if (strategy.RotateCityId) components.Add("rc");
        if (strategy.VaryQueryParams) components.Add("vq");
        if (strategy.AddSessionCookies) components.Add("sc");
        if (strategy.SimulateBrowserFingerprint) components.Add("bf");
        if (strategy.SimulateHumanTiming) components.Add("ht");
        if (strategy.CacheBusting) components.Add("cb");

        return string.Join("-", components);
    }
}
