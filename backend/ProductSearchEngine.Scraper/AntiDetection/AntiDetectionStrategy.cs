using System;

namespace ProductSearchEngine.Scraper.AntiDetection;

public class AntiDetectionStrategy
{
    public bool RotateUserAgent { get; set; } = true;
    public bool RandomHeaders { get; set; } = true;
    public bool RotateCityId { get; set; } = true;
    public bool VaryQueryParams { get; set; } = false;
    public bool AddSessionCookies { get; set; } = false;
    public bool RotateEndpoints { get; set; } = false;
    public bool SimulateBrowserFingerprint { get; set; } = false;
    public bool SimulateHumanTiming { get; set; } = true;
    public bool AddNavigationTiming { get; set; } = false;
    public bool AddReferrerPath { get; set; } = false;
    public bool CacheBusting { get; set; } = false;
    public bool VaryAcceptParams { get; set; } = false;
    public bool SimulateMediaFeatures { get; set; } = false;
    public bool RandomizeParamOrder { get; set; } = false;
    public bool SimulateBandwidth { get; set; } = false;

    // Timing configuration
    public int BaseDelayMs { get; set; } = 300;
    public int JitterPercentage { get; set; } = 30;
    public double RandomPauseProbability { get; set; } = 0.1;
    public int MaxPauseMs { get; set; } = 5000;

    // Request configuration
    public string SortOption { get; set; } = "relevance";
    public int PageNumber { get; set; } = 1;
    public int RequestSizeKb { get; set; } = 2;

    // Cache busting
    public bool AddCacheBusting { get; set; } = false;

    public static AntiDetectionStrategy CreateBasicStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            RotateCityId = true,
            SimulateHumanTiming = true,
            BaseDelayMs = 300,
            JitterPercentage = 30,
            RandomPauseProbability = 0.1,
            AddSessionCookies = true  // Enable session cookies for city selection
        };
    }

    public static AntiDetectionStrategy CreateAdvancedStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            RotateCityId = true,
            VaryQueryParams = true,
            AddSessionCookies = true,
            SimulateBrowserFingerprint = true,
            SimulateHumanTiming = true,
            CacheBusting = true,
            VaryAcceptParams = true,
            RandomizeParamOrder = true,
            BaseDelayMs = 400,
            JitterPercentage = 40,
            RandomPauseProbability = 0.15,
            MaxPauseMs = 8000
        };
    }

    public static AntiDetectionStrategy CreateStealthStrategy()
    {
        return new AntiDetectionStrategy
        {
            RotateUserAgent = true,
            RandomHeaders = true,
            RotateCityId = true,
            VaryQueryParams = true,
            AddSessionCookies = true,
            SimulateBrowserFingerprint = true,
            SimulateHumanTiming = true,
            AddNavigationTiming = true,
            AddReferrerPath = true,
            CacheBusting = true,
            VaryAcceptParams = true,
            SimulateMediaFeatures = true,
            RandomizeParamOrder = true,
            SimulateBandwidth = true,
            BaseDelayMs = 500,
            JitterPercentage = 50,
            RandomPauseProbability = 0.2,
            MaxPauseMs = 10000
        };
    }
}
