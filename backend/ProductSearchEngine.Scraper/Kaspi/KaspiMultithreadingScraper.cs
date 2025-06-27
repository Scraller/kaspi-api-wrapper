using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;

namespace ProductSearchEngine.Scraper.Kaspi
{
    /// <summary>
    /// Handles multithreaded scraping of Kaspi product pages with proper anti-detection measures
    /// </summary>
    public class KaspiMultithreadingScraper
    {
        private readonly KaspiHttpClient _httpClient;
        private readonly KaspiJsonParser _jsonParser;
        private readonly ApiRequestBuilder _apiRequestBuilder;
        private readonly KaspiHumanSimulator _humanSimulator;
        private readonly CityRotationManager _cityRotationManager;
        private readonly AntiDetectionManager _antiDetectionManager;
        private readonly ILogger _logger;

        // Sequential page processing constants
        private const int INITIAL_SEQUENTIAL_PAGES = 5;

        // Thread coordination
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<Product> _collectedProducts = new();
        private readonly ConcurrentDictionary<int, List<Product>> _pageProductsCache = new();
        private readonly ConcurrentDictionary<int, bool> _processedPages = new();
        private int _consecutiveEmptyPages = 0;
        private int _maxConsecutiveEmpty = 3;

        // Cancellation support
        private CancellationTokenSource _internalCts = new();
        private bool _isPaused = false;
        private readonly object _pauseLock = new();
        private int _isCancellationRequested = 0;

        // Status tracking
        private int _totalPagesProcessed = 0;
        private int _currentPage = 1;
        private int _estimatedTotalPages = 0;
        private int _lastProcessedPage = 0;
        private readonly ConcurrentDictionary<string, bool> _processedProductIds = new();

        public KaspiMultithreadingScraper(
            KaspiHttpClient httpClient,
            KaspiJsonParser jsonParser,
            ApiRequestBuilder apiRequestBuilder,
            KaspiHumanSimulator humanSimulator,
            CityRotationManager cityRotationManager,
            AntiDetectionManager antiDetectionManager,
            ILogger logger,
            int maxConcurrentRequests)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
            _apiRequestBuilder = apiRequestBuilder ?? throw new ArgumentNullException(nameof(apiRequestBuilder));
            _humanSimulator = humanSimulator ?? throw new ArgumentNullException(nameof(humanSimulator));
            _cityRotationManager = cityRotationManager ?? throw new ArgumentNullException(nameof(cityRotationManager));
            _antiDetectionManager = antiDetectionManager ?? throw new ArgumentNullException(nameof(antiDetectionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize semaphore with the maximum number of concurrent requests
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        }

        /// <summary>
        /// Scrapes products using a combination of sequential and multithreaded approaches
        /// </summary>
        /// <param name="category">The category to scrape</param>
        /// <param name="maxProducts">Maximum number of products to collect</param>
        /// <param name="externalCancellationToken">External cancellation token for interrupting the scraping</param>
        /// <returns>List of collected products</returns>
        public async Task<IEnumerable<Product>> ScrapeProductsAsync(
            string category,
            int maxProducts,
            CancellationToken externalCancellationToken = default)
        {
            // Reset state for new scraping session
            ResetState();

            // Calculate estimated total pages (Kaspi has 12 products per page)
            _estimatedTotalPages = (maxProducts / 12) + 1;

            // Create a linked token source that combines internal and external cancellation
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _internalCts.Token,
                externalCancellationToken);

            var cancellationToken = linkedCts.Token;

            try
            {
                // Phase 1: Process the first few pages sequentially to establish a valid scraping session
                await ProcessInitialPagesSequentially(category, maxProducts, cancellationToken);

                // Check if operation was cancelled during sequential phase
                cancellationToken.ThrowIfCancellationRequested();

                // Exit early if we already have enough products or encountered too many empty pages
                if (_collectedProducts.Count >= maxProducts || _consecutiveEmptyPages >= _maxConsecutiveEmpty)
                {
                    _logger.LogInformation("Completed after sequential phase with {ProductCount} products", _collectedProducts.Count);
                    return _collectedProducts.Take(maxProducts);
                }

                // Phase 2: Process remaining pages in parallel with proper referrer relationship
                await ProcessRemainingPagesInParallel(category, maxProducts, cancellationToken);

                // Return the collected products (limited to the requested maximum)
                return _collectedProducts.Take(maxProducts);
            }
            catch (OperationCanceledException)
            {
                // Create a snapshot of products collected at the moment of cancellation
                // This prevents including products that may be added by in-flight requests after cancellation
                // Use a lock-free approach by creating the snapshot immediately
                var productsSnapshot = new List<Product>();

                // Create thread-safe snapshot by iterating the concurrent collection
                foreach (var product in _collectedProducts)
                {
                    productsSnapshot.Add(product);
                }

                _logger.LogInformation("Scraping was interrupted with {ProductCount} products collected", productsSnapshot.Count);
                return productsSnapshot.Take(maxProducts);
            }
        }

        /// <summary>
        /// Requests cancellation of the scraping process
        /// </summary>
        public void Cancel()
        {
            try
            {
                if (!_internalCts.IsCancellationRequested)
                {
                    // Set cancellation flag atomically before cancelling token
                    Interlocked.Exchange(ref _isCancellationRequested, 1);

                    // Log before cancellation
                    _logger.LogInformation("Scraping cancellation requested. Products collected so far: {ProductCount}",
                        _collectedProducts.Count);

                    // Cancel all pending operations - this will signal all waiting tasks
                    _internalCts.Cancel();
                    _logger.LogInformation("Cancellation token signaled to all tasks");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scraping cancellation");
            }
        }

        /// <summary>
        /// Pauses the scraping process
        /// </summary>
        public void Pause()
        {
            lock (_pauseLock)
            {
                _isPaused = true;
                _logger.LogInformation("Scraping paused with {ProductCount} products collected", _collectedProducts.Count);
            }
        }

        /// <summary>
        /// Resumes the scraping process
        /// </summary>
        public void Resume()
        {
            lock (_pauseLock)
            {
                _isPaused = false;
                _logger.LogInformation("Scraping resumed");
            }
        }

        /// <summary>
        /// Gets the current scraping status
        /// </summary>
        public ScrapingStatus GetStatus()
        {
            return new ScrapingStatus
            {
                IsPaused = _isPaused,
                ProductCount = _collectedProducts.Count,
                CurrentPage = _currentPage,
                TotalPagesProcessed = _totalPagesProcessed,
                EstimatedTotalPages = _estimatedTotalPages,
                LastProcessedPage = _lastProcessedPage,
                CollectedProducts = _collectedProducts.ToList()
            };
        }

        /// <summary>
        /// Gets the current product count
        /// </summary>
        public int ProductCount => _collectedProducts.Count;

        /// <summary>
        /// Gets currently collected products
        /// </summary>
        public IEnumerable<Product> CollectedProducts => _collectedProducts.ToList();

        /// <summary>
        /// Resets the internal state for a new scraping session
        /// </summary>
        private void ResetState(bool preserveProgress = false)
        {
            // Reset cancellation token source if it's been used
            if (_internalCts.IsCancellationRequested)
            {
                _internalCts.Dispose();
                _internalCts = new CancellationTokenSource();
            }

            // Reset collections
            if (!preserveProgress)
            {
                _collectedProducts.Clear();
                _pageProductsCache.Clear();
                _processedPages.Clear();
                _processedProductIds.Clear();
            }

            // Reset counters
            _consecutiveEmptyPages = 0;
            _totalPagesProcessed = 0;
            _currentPage = 1;
            _isPaused = false;
        }

        /// <summary>
        /// Process the first few pages sequentially to establish session validity
        /// </summary>
        private async Task ProcessInitialPagesSequentially(string category, int maxProducts, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting sequential processing of initial {PageCount} pages", INITIAL_SEQUENTIAL_PAGES);

            for (int page = 1; page <= INITIAL_SEQUENTIAL_PAGES; page++)
            {
                // Check for cancellation request
                cancellationToken.ThrowIfCancellationRequested();

                // Check if paused and wait if necessary
                await CheckPausedAndWaitAsync(cancellationToken);

                _currentPage = page;
                var pageProducts = await ScrapePageAsync(category, page, page > 1 ? page - 1 : 0, cancellationToken);
                _totalPagesProcessed++;

                if (pageProducts.Any())
                {
                    // Cache the page products for referencing later
                    _pageProductsCache[page] = pageProducts.ToList();

                    // Add new products to the collection
                    foreach (var product in pageProducts)
                    {
                        if (!_collectedProducts.Any(p => p.Id == product.Id))
                        {
                            _collectedProducts.Add(product);

                            // Break if we've collected enough products
                            if (_collectedProducts.Count >= maxProducts)
                            {
                                _logger.LogInformation("Reached product limit during sequential phase");
                                return;
                            }
                        }
                    }

                    _consecutiveEmptyPages = 0;
                    _logger.LogInformation("Sequential page {Page}: Found {ProductCount} products", page, pageProducts.Count());
                }
                else
                {
                    _logger.LogWarning("Sequential page {Page}: No products found", page);
                    _consecutiveEmptyPages++;

                    if (_consecutiveEmptyPages >= _maxConsecutiveEmpty)
                    {
                        _logger.LogWarning("Too many consecutive empty pages during sequential phase, stopping");
                        return;
                    }
                }
            }

            _logger.LogInformation("Completed sequential processing with {ProductCount} products", _collectedProducts.Count);
        }

        /// <summary>
        /// <summary>
        /// Process remaining pages in parallel with proper referrer relationships
        /// </summary>
        private async Task ProcessRemainingPagesInParallel(string category, int maxProducts, CancellationToken cancellationToken, bool continuationMode = false)
        {
            _logger.LogInformation("Starting parallel processing of remaining pages" + (continuationMode ? " (continuation mode)" : ""));

            // Prepare the tasks list
            var tasks = new List<Task>();

            // Starting page number (after sequential phase or from continuation point)
            if (!continuationMode)
            {
                _currentPage = INITIAL_SEQUENTIAL_PAGES + 1;
            }

            // Track the last processed page for continuation
            _lastProcessedPage = Math.Max(_lastProcessedPage, _currentPage - 1);

            // Process pages until we have enough products or too many consecutive empty pages
            while (_collectedProducts.Count < maxProducts &&
                   _consecutiveEmptyPages < _maxConsecutiveEmpty &&
                   _isCancellationRequested == 0 &&
                   !cancellationToken.IsCancellationRequested)
            {
                // Check if paused and wait if necessary
                await CheckPausedAndWaitAsync(cancellationToken);

                // Calculate batch size based on referencing pattern, but also check cancellation
                var batchSize = Math.Min(INITIAL_SEQUENTIAL_PAGES, maxProducts - _collectedProducts.Count);

                // Double-check we haven't been cancelled before starting new tasks
                if (_isCancellationRequested == 1 || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Process a batch of pages
                for (int i = 0; i < batchSize && _collectedProducts.Count < maxProducts && _isCancellationRequested == 0; i++)
                {
                    int pageToProcess = _currentPage + i;
                    int referringPage = i + 1; // Pages 1-5 are used as referrers

                    // Skip if this page is already being processed
                    if (_processedPages.ContainsKey(pageToProcess))
                    {
                        continue;
                    }

                    _processedPages[pageToProcess] = false;

                    // Check cancellation before acquiring semaphore
                    if (_isCancellationRequested == 1 || cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Wait for semaphore to control concurrency
                    await _semaphore.WaitAsync(cancellationToken);

                    // Final check before creating task - prevent creating new tasks during cancellation
                    if (_isCancellationRequested == 1 || cancellationToken.IsCancellationRequested)
                    {
                        _semaphore.Release(); // Release the semaphore we just acquired
                        break;
                    }

                    // Add task to process this page
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            // Check for cancellation
                            cancellationToken.ThrowIfCancellationRequested();

                            // Check if paused and wait if necessary
                            await CheckPausedAndWaitAsync(cancellationToken);

                            var products = await ScrapePageAsync(category, pageToProcess, referringPage, cancellationToken);

                            // Mark as processed to avoid duplicate work
                            _processedPages[pageToProcess] = true;
                            Interlocked.Increment(ref _totalPagesProcessed);

                            // Add new products to the collection only if not cancelled
                            foreach (var product in products)
                            {
                                // Double-check cancellation before adding any products
                                // Use the atomic cancellation flag for immediate response
                                if (_isCancellationRequested == 1 ||
                                    cancellationToken.IsCancellationRequested)
                                {
                                    break; // Stop adding products immediately if cancelled
                                }

                                if (!_collectedProducts.Any(p => p.Id == product.Id))
                                {
                                    _collectedProducts.Add(product);
                                }
                            }

                            if (products.Any() && _isCancellationRequested == 0 && !cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogInformation("Parallel page {Page}: Found {ProductCount} products. Total: {TotalCount}",
                                    pageToProcess, products.Count(), _collectedProducts.Count);

                                Interlocked.Exchange(ref _consecutiveEmptyPages, 0);
                            }
                            else if (!products.Any() && _isCancellationRequested == 0 && !cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogWarning("Parallel page {Page}: No products found", pageToProcess);
                                Interlocked.Increment(ref _consecutiveEmptyPages);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected exception when canceled
                            _logger.LogInformation("Page processing canceled for page {Page}", pageToProcess);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing page {Page}", pageToProcess);
                        }
                        finally
                        {
                            // Always release the semaphore
                            _semaphore.Release();
                        }
                    }, cancellationToken));
                }

                // Advance to the next batch of pages
                _currentPage += batchSize;

                // Wait for some tasks to complete before proceeding
                if (tasks.Any())
                {
                    // Wait for any task to complete
                    await Task.WhenAny(tasks.ToArray());

                    // Remove completed tasks
                    tasks.RemoveAll(t => t.IsCompleted);
                }

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Wait for all remaining tasks to complete or be canceled with timeout
            try
            {
                // Wait for all tasks but with a reasonable timeout to avoid hanging
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                _logger.LogInformation("All parallel tasks completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Remaining tasks canceled during cleanup - this is expected during interruption");

                // Count how many tasks are still running
                var runningTasks = tasks.Count(t => !t.IsCompleted);
                if (runningTasks > 0)
                {
                    _logger.LogInformation("Waiting for {RunningTasks} tasks to finish...", runningTasks);

                    // Give them a bit more time to clean up gracefully
                    try
                    {
                        await Task.Delay(2000, CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore any delays during shutdown
                    }
                }
            }

            _logger.LogInformation("Completed parallel processing with {ProductCount} products", _collectedProducts.Count);
        }

        /// <summary>
        /// Checks if scraping is paused and waits until resumed or canceled
        /// </summary>
        private async Task CheckPausedAndWaitAsync(CancellationToken cancellationToken)
        {
            bool isPaused;

            lock (_pauseLock)
            {
                isPaused = _isPaused;
            }

            while (isPaused && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500, cancellationToken);

                lock (_pauseLock)
                {
                    isPaused = _isPaused;
                }
            }

            // Check for cancellation after pausing
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Scrapes a single page with proper referrer
        /// </summary>
        private async Task<IEnumerable<Product>> ScrapePageAsync(
            string category,
            int pageNumber,
            int referringPageNumber,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Select an anti-detection strategy
                var strategy = _antiDetectionManager.SelectStrategy();
                strategy.PageNumber = pageNumber;
                strategy.AddReferrerPath = true; // Enable referrer

                // Set the referring city
                var cityId = _cityRotationManager.GetNextCityId();

                // Apply human-like timing
                await _humanSimulator.SimulateHumanTiming(strategy);

                // Build the API URL
                var apiUrl = _apiRequestBuilder.BuildApiUrl(category, cityId, strategy, pageNumber);

                // Customize the HTTP request to include the correct referrer
                Dictionary<string, string>? customHeaders = null;

                // Only add custom referrer if we're referring to a previous page
                if (referringPageNumber > 0)
                {
                    customHeaders = new Dictionary<string, string>();

                    // Create a referrer URL that points to the previous page
                    string referrerUrl = $"https://kaspi.kz/shop/c/{category}?page={referringPageNumber}";
                    customHeaders["Referer"] = referrerUrl;

                    _logger.LogDebug("Page {Page} using referrer from page {ReferringPage}: {Referrer}",
                        pageNumber, referringPageNumber, referrerUrl);
                }

                // Send the HTTP request
                var (success, jsonContent) = await _httpClient.SendRequestAsync(
                    apiUrl,
                    strategy,
                    cityId,
                    useSessionCookies: true,
                    customHeaders: customHeaders);

                if (success && !string.IsNullOrEmpty(jsonContent))
                {
                    // Parse the response
                    var products = _jsonParser.ParseProductsFromApiResponse(jsonContent, category);

                    // Record success for this strategy and city
                    _antiDetectionManager.RecordStrategyResult(strategy, products.Any());

                    if (products.Any())
                    {
                        _cityRotationManager.ReportCitySuccess(cityId);
                    }
                    else
                    {
                        _cityRotationManager.ReportCityFailure(cityId);
                    }

                    return products;
                }
                else
                {
                    _antiDetectionManager.RecordStrategyResult(strategy, false);
                    _cityRotationManager.ReportCityFailure(cityId);
                    return Enumerable.Empty<Product>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping page {Page}", pageNumber);
                return Enumerable.Empty<Product>();
            }
        }

        /// <summary>
        /// Scrapes products starting from a specific page, useful for continuing after interruption
        /// </summary>
        /// <param name="category">The category to scrape</param>
        /// <param name="maxProducts">Maximum number of products to collect</param>
        /// <param name="startFromPage">The page to start scraping from</param>
        /// <param name="existingProducts">Existing products to avoid duplicates</param>
        /// <param name="externalCancellationToken">External cancellation token for interrupting the scraping</param>
        /// <returns>List of collected products</returns>
        public async Task<IEnumerable<Product>> ContinueScrapingAsync(
            string category,
            int maxProducts,
            int startFromPage,
            IEnumerable<Product> existingProducts,
            CancellationToken externalCancellationToken = default)
        {
            // Reset state but keep track of what we've already processed
            ResetState(preserveProgress: true);

            // Register existing product IDs to avoid duplicates
            foreach (var product in existingProducts)
            {
                _processedProductIds[product.Id] = true;
                _collectedProducts.Add(product);
            }

            // Set starting page and last processed page
            _currentPage = Math.Max(startFromPage, INITIAL_SEQUENTIAL_PAGES + 1);
            _lastProcessedPage = Math.Max(_lastProcessedPage, _currentPage - 1);

            // Calculate estimated total pages (Kaspi has 12 products per page)
            _estimatedTotalPages = (maxProducts / 12) + 1;

            // Create a linked token source that combines internal and external cancellation
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _internalCts.Token,
                externalCancellationToken);

            var cancellationToken = linkedCts.Token;

            try
            {
                // Skip the sequential phase since we're continuing
                _logger.LogInformation("Continuing scraping from page {Page}", _currentPage);

                // Process remaining pages in parallel with proper referrer relationship
                await ProcessRemainingPagesInParallel(category, maxProducts, cancellationToken, continuationMode: true);

                // Return the collected products (limited to the requested maximum)
                return _collectedProducts.Take(maxProducts);
            }
            catch (OperationCanceledException)
            {
                // Create a snapshot of products collected at the moment of cancellation
                // This prevents including products that may be added by in-flight requests after cancellation
                // Use a lock-free approach by creating the snapshot immediately
                var productsSnapshot = new List<Product>();

                // Create thread-safe snapshot by iterating the concurrent collection
                foreach (var product in _collectedProducts)
                {
                    productsSnapshot.Add(product);
                }

                _logger.LogInformation("Scraping was interrupted with {ProductCount} products collected", productsSnapshot.Count);
                return productsSnapshot.Take(maxProducts);
            }
        }
    }

    /// <summary>
    /// Represents the current status of a scraping operation
    /// </summary>
    public class ScrapingStatus
    {
        public bool IsPaused { get; set; }
        public int ProductCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPagesProcessed { get; set; }
        public int EstimatedTotalPages { get; set; }
        public int LastProcessedPage { get; set; }
        public List<Product> CollectedProducts { get; set; } = new List<Product>();
    }
}
