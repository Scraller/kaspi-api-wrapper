using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper;
using ProductSearchEngine.Scraper.Configuration;

namespace ProductSearchEngine.Tests.Helpers;

/// <summary>
/// Base class for all tests providing common setup and utilities
/// </summary>
public abstract class TestBase
{
    // Use thread-safe storage for parallel execution
    private static readonly ThreadLocal<ServiceProvider?> _threadLocalServiceProvider = new();
    private ServiceProvider? _instanceServiceProvider;

    protected ServiceProvider ServiceProvider
    {
        get
        {
            // For parallel execution, use instance-based service provider
            if (_instanceServiceProvider == null)
            {
                _instanceServiceProvider = BuildServiceProvider();
            }
            return _instanceServiceProvider;
        }
    }
    protected ILogger<TestBase> Logger { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        // This runs once before all tests in the class
        // Initialize instance service provider for this test class
        Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        // This runs once after all tests in the class
        _instanceServiceProvider?.Dispose();
        _instanceServiceProvider = null;
    }

    /// <summary>
    /// Call this at the very end of all tests to clean up static resources
    /// </summary>
    public static void GlobalCleanup()
    {
        // Clean up any remaining thread-local providers
        _threadLocalServiceProvider.Dispose();
    }

    [SetUp]
    public virtual void SetUp()
    {
        // This runs before each test
        Logger.LogInformation("Starting test: {TestName}", TestContext.CurrentContext.Test.Name);
    }

    [TearDown]
    public virtual void TearDown()
    {
        // This runs after each test
        Logger.LogInformation("Completed test: {TestName} with result: {Result}",
            TestContext.CurrentContext.Test.Name,
            TestContext.CurrentContext.Result.Outcome);
    }

    /// <summary>
    /// Builds service provider with test configuration
    /// </summary>
    protected virtual ServiceProvider BuildServiceProvider(bool fullCategoryScrape = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        // Configure logging for tests
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register HTTP client
        services.AddHttpClient("KaspiScraper", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHttpClient();

        // Register CityRotationManager for dynamic city ID management
        services.AddSingleton<Scraper.AntiDetection.CityRotationManager>();

        // Get city IDs from CityRotationManager instance
        var cityRotationManager = new Scraper.AntiDetection.CityRotationManager();
        var cityIds = cityRotationManager.GetAllCityIds().ToList();

        // Add configuration for KaspiScraperConfig
        services.Configure<KaspiScraperConfig>(config =>
        {
            if (fullCategoryScrape)
            {
                // More aggressive configuration for full category scrape
                config.MaxRetries = 5;
                config.DelayBetweenRequestsMs = 1000;
                config.JitterPercentage = 30.0;
                config.MaxConcurrentRequests = 2;
            }
            else
            {
                // Test-friendly configuration
                config.MaxRetries = 2;
                config.DelayBetweenRequestsMs = 200; // Faster for tests
                config.JitterPercentage = 10.0;
                config.MaxConcurrentRequests = 3;
            }

            // Use CityRotationManager's city IDs instead of hardcoding them
            config.CityIds = cityIds;
        });

        // Register the scraper service directly
        services.AddScoped<KaspiApiScraperEnhanced>();

        // Register Kaspi scraper dependencies
        services.AddScoped<Scraper.AntiDetection.HeaderGenerator>();
        services.AddScoped<Scraper.AntiDetection.RequestThrottler>();
        services.AddScoped<Scraper.Kaspi.SessionManager>();

        // Register KaspiHttpClient with proper dependencies
        services.AddScoped(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<Scraper.Kaspi.KaspiHttpClient>>();
            var headerGenerator = provider.GetRequiredService<Scraper.AntiDetection.HeaderGenerator>();
            var requestThrottler = provider.GetRequiredService<Scraper.AntiDetection.RequestThrottler>();
            var sessionManager = provider.GetRequiredService<Scraper.Kaspi.SessionManager>();
            return new Scraper.Kaspi.KaspiHttpClient(httpClientFactory, logger, headerGenerator, requestThrottler, sessionManager);
        });

        // Register KaspiOfferApiClient with proper dependencies
        services.AddScoped(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger<Scraper.Kaspi.KaspiOfferApiClient>>();
            var httpClient = provider.GetRequiredService<Scraper.Kaspi.KaspiHttpClient>();
            return new Scraper.Kaspi.KaspiOfferApiClient(httpClientFactory, logger, httpClient);
        });

        // Register KaspiNavigationApiClient with proper dependencies
        services.AddScoped(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Scraper.Services.KaspiNavigationApiClient>>();
            var httpClient = provider.GetRequiredService<Scraper.Kaspi.KaspiHttpClient>();
            return new Scraper.Services.KaspiNavigationApiClient(httpClient, logger);
        });

        // Register anti-detection strategy
        services.AddScoped<Scraper.AntiDetection.AntiDetectionStrategy>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the scraper service for testing
    /// </summary>
    protected KaspiApiScraperEnhanced GetScraperService()
    {
        return ServiceProvider.GetRequiredService<KaspiApiScraperEnhanced>();
    }
}
