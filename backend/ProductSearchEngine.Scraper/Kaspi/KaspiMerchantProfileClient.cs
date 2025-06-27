using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi;

/// <summary>
/// Client for extracting merchant profile data from Kaspi.kz merchant pages.
/// Based on Phase 2 discovery findings: merchant data is embedded in window.BACKEND.components.merchant
/// </summary>
public class KaspiMerchantProfileClient
{ 
    private readonly KaspiHttpClient _httpClient;
    private readonly ILogger<KaspiMerchantProfileClient> _logger;
    
    private const string MERCHANT_PROFILE_URL = "https://kaspi.kz/shop/info/merchant/{0}/address-tab/";
    
    /// <summary>
    /// Regex pattern to extract BACKEND.components.merchant object from HTML
    /// Based on Phase 2 discovery: merchant data is in JavaScript object, not script tag JSON
    /// </summary>
    private readonly Regex _merchantDataRegex = new(@"BACKEND\.components\.merchant\s*=\s*(\{[^;]+\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public KaspiMerchantProfileClient(KaspiHttpClient httpClient, ILogger<KaspiMerchantProfileClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts merchant profile data from Kaspi merchant page.
    /// Based on Phase 2 discovery findings with verified data structure.
    /// </summary>
    /// <param name="merchantId">Merchant ID (can be string like "Sulpak" or numeric like "30352665")</param>
    /// <returns>Merchant embedded data or null if not found</returns>
    public async Task<MerchantEmbeddedData?> GetMerchantProfileAsync(string merchantId)
    {
        try
        {
            _logger.LogInformation("Fetching merchant profile for ID: {MerchantId}", merchantId);
            
            var url = string.Format(MERCHANT_PROFILE_URL, merchantId);
            _logger.LogDebug("Requesting URL: {Url}", url);
            
            // Create anti-detection strategy for merchant profile pages
            var strategy = new AntiDetectionStrategy
            {
                RotateUserAgent = true,
                RandomHeaders = true,
                RotateCityId = false, // Use consistent city for merchant profiles
                SimulateHumanTiming = true,
                AddSessionCookies = true,
                BaseDelayMs = 1000,
                JitterPercentage = 50,
                RandomPauseProbability = 0.1,
                MaxPauseMs = 3000
            };
            
            var customHeaders = new Dictionary<string, string>
            {
                ["Referer"] = "https://kaspi.kz/",
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                ["Accept-Language"] = "ru-RU,ru;q=0.9,en;q=0.8",
                ["Accept-Encoding"] = "gzip, deflate, br",
                ["DNT"] = "1",
                ["Connection"] = "keep-alive",
                ["Upgrade-Insecure-Requests"] = "1"
            };
            
            // Use default city (Almaty) for merchant profile requests
            var (success, html) = await _httpClient.SendRequestAsync(url, strategy, "750000000", true, customHeaders);
            
            if (!success || string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Empty or failed response for merchant ID: {MerchantId}", merchantId);
                return null;
            }

            // Extract BACKEND.components.merchant object using regex
            var match = _merchantDataRegex.Match(html);
            if (!match.Success)
            {
                _logger.LogWarning("Could not find BACKEND.components.merchant object in HTML for merchant ID: {MerchantId}", merchantId);
                return null;
            }

            var jsonData = match.Groups[1].Value;
            _logger.LogDebug("Extracted merchant JSON: {JsonData}", jsonData);

            // Deserialize the merchant data
            var merchantData = JsonSerializer.Deserialize<MerchantEmbeddedData>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (merchantData == null)
            {
                _logger.LogWarning("Failed to deserialize merchant data for ID: {MerchantId}", merchantId);
                return null;
            }

            _logger.LogInformation("Successfully extracted merchant data for {MerchantName} (ID: {MerchantId}), Rating: {Rating}, Reviews: {ReviewCount}, Sales: {SalesCount}", 
                merchantData.Name, merchantData.Uid, merchantData.Rating, merchantData.NumberOfReviews, merchantData.SalesCount);

            return merchantData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching merchant profile for ID: {MerchantId}", merchantId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for merchant ID: {MerchantId}", merchantId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching merchant profile for ID: {MerchantId}", merchantId);
            return null;
        }
    }
}

/// <summary>
/// Merchant embedded data structure based on Phase 2 discovery findings.
/// Represents the actual data structure found in window.BACKEND.components.merchant
/// </summary>
public class MerchantEmbeddedData
{
    /// <summary>
    /// Merchant unique identifier (can be string like "Sulpak" or numeric like "30352665")
    /// </summary>
    public string Uid { get; set; } = string.Empty;
    
    /// <summary>
    /// Full merchant name as displayed on Kaspi
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Logo URL (can be null for some merchants)
    /// </summary>
    public string? Logo { get; set; }
    
    /// <summary>
    /// Phone number (verified present in Phase 2 discovery)
    /// </summary>
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant registration/creation date
    /// </summary>
    public DateTime Create { get; set; }
    
    /// <summary>
    /// Total sales count (ALWAYS PRESENT per Phase 2 verification)
    /// </summary>
    public int SalesCount { get; set; }
    
    /// <summary>
    /// Number of customer reviews (verified accurate in Phase 2)
    /// </summary>
    public int NumberOfReviews { get; set; }
    
    /// <summary>
    /// Customer rating from 0-5 (verified accurate in Phase 2)
    /// </summary>
    public decimal Rating { get; set; }
}
