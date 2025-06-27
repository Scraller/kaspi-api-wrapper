using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Configuration;
using ProductSearchEngine.Scraper.Kaspi;

namespace ProductSearchEngine.Scraper;

/// <summary>
/// Enhanced implementation of Kaspi API scraper with advanced anti-detection
/// and performance optimizations.
/// </summary>
public class KaspiApiScraperEnhanced : IDisposable
{
    // API constants
    private const string API_BASE_URL = "https://kaspi.kz/yml/product-view/pl/results";
    private const string PRODUCT_DETAIL_URL = "https://kaspi.kz/yml/product-view/main/";

    // Dependencies
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KaspiScraperConfig _config;
    private readonly ILogger<KaspiApiScraperEnhanced> _logger;

    // Component services
    private readonly KaspiHttpClient _httpClient;
    private readonly KaspiJsonParser _jsonParser;
    private readonly KaspiProductEnricher _productEnricher;
    private readonly KaspiPerformanceLogger _performanceLogger;
    private readonly KaspiHumanSimulator _humanSimulator;
    private readonly KaspiMultithreadingScraper _multithreadingScraper;
    private readonly KaspiOfferApiClient _offerApiClient;

    // Anti-detection components
    private readonly AntiDetectionManager _antiDetectionManager;
    private readonly HeaderGenerator _headerGenerator;
    private readonly CityRotationManager _cityRotationManager;
    private readonly RequestThrottler _requestThrottler;
    private readonly ApiRequestBuilder _apiRequestBuilder;
    private readonly SessionManager _sessionManager;

    // State management
    private bool _disposed = false;
    private readonly Random _random = new();
    private CancellationTokenSource? _currentScrapingCts;
    private bool _isScrapingInProgress = false;

    public string MarketplaceName => "kaspi";

    // Events for subscribers to get scraping updates
    public event EventHandler<ScrapingProgressEventArgs>? ScrapingProgressUpdated;
    public event EventHandler<ScrapingInterruptedEventArgs>? ScrapingInterrupted;

    public KaspiApiScraperEnhanced(
        IHttpClientFactory httpClientFactory,
        IOptions<KaspiScraperConfig> config,
        ILogger<KaspiApiScraperEnhanced> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize anti-detection components
        _antiDetectionManager = new AntiDetectionManager();
        _headerGenerator = new HeaderGenerator();
        _cityRotationManager = new CityRotationManager();
        _requestThrottler = new RequestThrottler(4); // Use a reasonable default value
        _apiRequestBuilder = new ApiRequestBuilder();
        _sessionManager = new SessionManager();

        // Initialize component services
        _jsonParser = new KaspiJsonParser(_logger, MarketplaceName);
        _humanSimulator = new KaspiHumanSimulator();
        _performanceLogger = new KaspiPerformanceLogger(_logger);

        // Components with dependencies
        _httpClient = new KaspiHttpClient(
            _httpClientFactory,
            _logger,
            _headerGenerator,
            _requestThrottler,
            _sessionManager);

        _productEnricher = new KaspiProductEnricher(
            _logger,
            _httpClient,
            _jsonParser,
            _humanSimulator);

        // Initialize multithreading scraper
        _multithreadingScraper = new KaspiMultithreadingScraper(
            _httpClient,
            _jsonParser,
            _apiRequestBuilder,
            _humanSimulator,
            _cityRotationManager,
            _antiDetectionManager,
            _logger,
            _config.MaxConcurrentRequests);

        _offerApiClient = new KaspiOfferApiClient(
            _httpClientFactory,
            _logger,
            _httpClient);
    }

    public async Task<IEnumerable<Product>> ScrapeProductsAsync(string query, int maxProducts = 10)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var performanceMetrics = new Dictionary<string, long>();

        _logger.LogInformation("Starting enhanced Kaspi API scraping for query: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogError("Invalid empty query for Kaspi scraping");
            return Enumerable.Empty<Product>();
        }

        // Map category to API format
        var apiCategory = _apiRequestBuilder.MapCategoryToApiFormat(query);

        // Determine whether to use multithreading based on the number of products requested
        bool useMultithreading = maxProducts > 20;

        // Set up cancellation token for interruption
        _currentScrapingCts = new CancellationTokenSource();
        _isScrapingInProgress = true;

        IEnumerable<Product> products;

        try
        {
            if (useMultithreading)
            {
                _logger.LogInformation("Using multithreaded scraping for {MaxProducts} products", maxProducts);

                // Start progress reporting task for multithreaded scraping
                _ = Task.Run(() => ReportScrapingProgressAsync(_currentScrapingCts.Token));

                // Use the multithreaded scraper with page referrer logic
                var multithreadStopwatch = Stopwatch.StartNew();
                products = await _multithreadingScraper.ScrapeProductsAsync(
                    apiCategory,
                    maxProducts,
                    _currentScrapingCts.Token);

                multithreadStopwatch.Stop();

                performanceMetrics["multithreaded_scraping_ms"] = multithreadStopwatch.ElapsedMilliseconds;
            }
            else
            {
                _logger.LogInformation("Using sequential scraping for {MaxProducts} products", maxProducts);

                // Use the original sequential approach for smaller product counts
                products = await ScrapeProductsSequentiallyAsync(
                    apiCategory,
                    maxProducts,
                    performanceMetrics,
                    _currentScrapingCts.Token);
            }

            totalStopwatch.Stop();

            // Log performance metrics
            _performanceLogger.LogPerformanceMetrics(totalStopwatch.ElapsedMilliseconds, products.Count(), performanceMetrics);

            _logger.LogInformation("Kaspi API scraping completed. Products: {ProductCount}, Time: {TimeMs}ms",
                products.Count(), totalStopwatch.ElapsedMilliseconds);

            // Enrich product details if needed
            if (products.Any() && products.Count() <= 5) // Only for small result sets
            {
                var strategy = _antiDetectionManager.SelectStrategy();
                var cityId = _cityRotationManager.GetNextCityId();
                await _productEnricher.EnrichProductDetails(products.ToList(), strategy, cityId);
            }

            return products;
        }
        catch (OperationCanceledException)
        {
            totalStopwatch.Stop();

            // Get collected products so far (take a snapshot to prevent race conditions)
            var collectedProducts = _multithreadingScraper.CollectedProducts.ToList();
            var limitedProducts = collectedProducts.Take(maxProducts).ToList();

            _logger.LogInformation("Scraping was interrupted. Products collected: {ProductCount}",
                limitedProducts.Count);

            // Raise event to notify about the interruption
            OnScrapingInterrupted(limitedProducts);

            return limitedProducts;
        }
        finally
        {
            _isScrapingInProgress = false;

            // Dispose the cancellation token source
            if (_currentScrapingCts != null)
            {
                _currentScrapingCts.Dispose();
                _currentScrapingCts = null;
            }
        }
    }

    /// <summary>
    /// Original sequential scraping approach (for smaller product counts)
    /// </summary>
    private async Task<IEnumerable<Product>> ScrapeProductsSequentiallyAsync(
        string category,
        int maxProducts,
        Dictionary<string, long> performanceMetrics,
        CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        int retryCount = 0;
        int currentPage = 1;
        int consecutiveEmptyPages = 0;
        const int maxConsecutiveEmpty = 3;

        // Generate a session ID for consistency across requests
        var sessionId = _sessionManager.GenerateSessionId();

        while (products.Count < maxProducts && retryCount <= _config.MaxRetries && consecutiveEmptyPages < maxConsecutiveEmpty)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Select anti-detection strategy
                var strategy = _antiDetectionManager.SelectStrategy();
                strategy.PageNumber = currentPage;

                // Get current city ID with rotation
                var cityId = _cityRotationManager.GetNextCityId();

                // Apply human-like timing before request
                await _humanSimulator.SimulateHumanTiming(strategy);

                // Build API URL with proper params
                var urlBuildStopwatch = Stopwatch.StartNew();
                var apiUrl = _apiRequestBuilder.BuildApiUrl(category, cityId, strategy, currentPage);
                urlBuildStopwatch.Stop();
                performanceMetrics["url_build_ms"] = urlBuildStopwatch.ElapsedMilliseconds;

                _logger.LogDebug("API URL: {Url}", apiUrl);

                // Send HTTP request with anti-detection measures
                var httpStopwatch = Stopwatch.StartNew();
                var (success, jsonContent) = await _httpClient.SendRequestAsync(apiUrl, strategy, cityId);
                httpStopwatch.Stop();
                performanceMetrics[$"http_request_page_{currentPage}_ms"] = httpStopwatch.ElapsedMilliseconds;

                if (success && !string.IsNullOrEmpty(jsonContent))
                {
                    // Parse the API response
                    var parseStopwatch = Stopwatch.StartNew();
                    var pageProducts = _jsonParser.ParseProductsFromApiResponse(jsonContent, category);
                    parseStopwatch.Stop();
                    performanceMetrics[$"parsing_page_{currentPage}_ms"] = parseStopwatch.ElapsedMilliseconds;

                    if (pageProducts != null && pageProducts.Any())
                    {
                        // Process products
                        var newProducts = pageProducts.Where(p => !products.Any(existing => existing.Id == p.Id)).ToList();
                        var addedCount = Math.Min(newProducts.Count, maxProducts - products.Count);
                        products.AddRange(newProducts.Take(addedCount));

                        _logger.LogInformation($"Page {currentPage}: Found {pageProducts.Count()} products, added {addedCount} new ones");

                        // Record successful strategy
                        _antiDetectionManager.RecordStrategyResult(strategy, true);
                        _cityRotationManager.ReportCitySuccess(cityId);

                        // Reset counters on success
                        retryCount = 0;
                        consecutiveEmptyPages = 0;
                        currentPage++;

                        // If we have enough products, break
                        if (products.Count >= maxProducts)
                        {
                            break;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Page {currentPage}: No products found");
                        consecutiveEmptyPages++;

                        // If we get no results, try next page with different city
                        if (consecutiveEmptyPages < maxConsecutiveEmpty)
                        {
                            currentPage++;
                            _cityRotationManager.GetNextCityId(forceRotation: true);
                        }

                        _antiDetectionManager.RecordStrategyResult(strategy, false);
                    }
                }
                else
                {
                    // Handle non-success status codes
                    _logger.LogWarning($"Failed request on page {currentPage}");

                    // Report city failure
                    _cityRotationManager.ReportCityFailure(cityId);
                    _antiDetectionManager.RecordStrategyResult(strategy, false);

                    retryCount++;
                    if (retryCount <= _config.MaxRetries)
                    {
                        // Exponential backoff
                        var backoffMs = (int)(Math.Pow(2, retryCount) * 1000);
                        _logger.LogInformation($"Backing off for {backoffMs}ms before retry {retryCount}/{_config.MaxRetries}");
                        await Task.Delay(backoffMs, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API scraping on page {Page}", currentPage);

                retryCount++;
                if (retryCount <= _config.MaxRetries)
                {
                    var backoffMs = (int)(Math.Pow(2, retryCount) * 1000);
                    await Task.Delay(backoffMs, cancellationToken);
                }
            }
        }

        return products;
    }

    public async Task<Product?> ScrapeProductDetailsAsync(string productUrl)
    {
        _logger.LogInformation("Scraping product details for URL: {Url}", productUrl);

        int retryCount = 0;

        while (retryCount <= _config.MaxRetries)
        {
            try
            {
                var strategy = _antiDetectionManager.SelectStrategy();
                var cityId = _cityRotationManager.GetNextCityId();

                var product = await _productEnricher.ScrapeProductDetailsAsync(productUrl, strategy, cityId);

                if (product != null)
                {
                    _antiDetectionManager.RecordStrategyResult(strategy, true);
                    _cityRotationManager.ReportCitySuccess(cityId);
                    return product;
                }

                _antiDetectionManager.RecordStrategyResult(strategy, false);

                retryCount++;
                if (retryCount <= _config.MaxRetries)
                {
                    var backoffMs = (int)(Math.Pow(2, retryCount) * 1000);
                    await Task.Delay(backoffMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping product details for URL: {ProductUrl}", productUrl);

                retryCount++;
                if (retryCount <= _config.MaxRetries)
                {
                    var backoffMs = (int)(Math.Pow(2, retryCount) * 1000);
                    await Task.Delay(backoffMs);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Scrapes all offers for a specific product using the Kaspi offer API
    /// </summary>
    public async Task<IEnumerable<Offer>> ScrapeOffersForProductAsync(string productId, string? cityId = null)
    {
        _logger.LogInformation("Scraping offers for product: {ProductId}", productId);

        var selectedCityId = cityId ?? _cityRotationManager.GetNextCityId();
        var strategy = _antiDetectionManager.SelectStrategy();

        try
        {
            var offers = await _offerApiClient.GetOffersForProductAsync(productId, selectedCityId, strategy);

            _logger.LogInformation("Found {OfferCount} offers for product {ProductId}", offers.Count(), productId);

            return offers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping offers for product {ProductId}", productId);
            return Enumerable.Empty<Offer>();
        }
    }

    /// <summary>
    /// Pauses the current scraping operation
    /// </summary>
    public void PauseScraping()
    {
        if (_isScrapingInProgress)
        {
            _multithreadingScraper.Pause();
            _logger.LogInformation("Scraping paused by user");
        }
    }

    /// <summary>
    /// Resumes a paused scraping operation
    /// </summary>
    public void ResumeScraping()
    {
        if (_isScrapingInProgress)
        {
            _multithreadingScraper.Resume();
            _logger.LogInformation("Scraping resumed by user");
        }
    }

    /// <summary>
    /// Cancels the current scraping operation
    /// </summary>
    public void CancelScraping()
    {
        if (_isScrapingInProgress && _currentScrapingCts != null && !_currentScrapingCts.IsCancellationRequested && !_disposed)
        {
            try
            {
                _currentScrapingCts.Cancel();
                _logger.LogInformation("Scraping canceled by user");
            }
            catch (ObjectDisposedException)
            {
                // The cancellation token source was already disposed - this is fine
                _logger.LogDebug("Cancellation token source was already disposed");
            }
        }
    }

    /// <summary>
    /// Gets the current scraping status
    /// </summary>
    public ScrapingStatus GetScrapingStatus()
    {
        if (_isScrapingInProgress)
        {
            return _multithreadingScraper.GetStatus();
        }

        return new ScrapingStatus
        {
            IsPaused = false,
            ProductCount = 0,
            CurrentPage = 0,
            TotalPagesProcessed = 0,
            EstimatedTotalPages = 0
        };
    }

    /// <summary>
    /// Reports scraping progress at regular intervals
    /// </summary>
    private async Task ReportScrapingProgressAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Get current status
                var status = _multithreadingScraper.GetStatus();

                // Report progress
                OnScrapingProgressUpdated(status);

                // Wait before next update
                await Task.Delay(2000, cancellationToken);
            }

            // One final update with cancellation status
            if (_isScrapingInProgress)
            {
                var finalStatus = _multithreadingScraper.GetStatus();
                OnScrapingProgressUpdated(finalStatus);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when canceled
            // Report final state if still in progress
            if (_isScrapingInProgress)
            {
                var finalStatus = _multithreadingScraper.GetStatus();
                OnScrapingProgressUpdated(finalStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting scraping progress");
        }
    }

    /// <summary>
    /// Raises the scraping progress updated event
    /// </summary>
    protected virtual void OnScrapingProgressUpdated(ScrapingStatus status)
    {
        var isCancellationRequested = _currentScrapingCts?.IsCancellationRequested ?? false;
        ScrapingProgressUpdated?.Invoke(this, new ScrapingProgressEventArgs(status, isCancellationRequested));
    }

    /// <summary>
    /// Raises the scraping interrupted event
    /// </summary>
    protected virtual void OnScrapingInterrupted(IEnumerable<Product> products)
    {
        ScrapingInterrupted?.Invoke(this, new ScrapingInterruptedEventArgs(products));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cancel any ongoing scraping before disposing resources
                if (_isScrapingInProgress)
                {
                    try
                    {
                        if (_currentScrapingCts != null && !_currentScrapingCts.IsCancellationRequested)
                        {
                            _currentScrapingCts.Cancel();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed - ignore
                    }
                }

                // Dispose managed resources
                _currentScrapingCts?.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Event arguments for scraping progress updates
/// </summary>
public class ScrapingProgressEventArgs : EventArgs
{
    public ScrapingStatus Status { get; }
    public bool IsCancellationRequested { get; }

    public ScrapingProgressEventArgs(ScrapingStatus status, bool isCancellationRequested = false)
    {
        Status = status;
        IsCancellationRequested = isCancellationRequested;
    }
}

/// <summary>
/// Event arguments for scraping interruption
/// </summary>
public class ScrapingInterruptedEventArgs : EventArgs
{
    public IEnumerable<Product> CollectedProducts { get; }

    public ScrapingInterruptedEventArgs(IEnumerable<Product> products)
    {
        CollectedProducts = products;
    }
}
