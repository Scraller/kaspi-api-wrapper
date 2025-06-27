using ProductSearchEngine.Scraper.Configuration;
using ProductSearchEngine.Scraper.Services;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Api.Middleware;
using ProductSearchEngine.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Kaspi.kz API Wrapper",
        Version = "v1",
        Description = @"A clean, documented REST API wrapper around Kaspi.kz's existing APIs for accessing marketplace data.

## Features
- **Category Hierarchy**: Access to complete Kaspi.kz category structure with subcategories
- **Product Search**: Search products with various filters and sorting options
- **Product Details**: Get detailed product information including offers and pricing
- **Market Navigation**: Navigate through marketplace structure using official APIs
- **Rate Limiting Protection**: Built-in protection against rate limiting with intelligent retry

## Open Source
This API wrapper is designed to provide developers with clean, documented access to Kaspi.kz data through standardized REST endpoints with comprehensive Swagger documentation.

## No Data Storage
This is a stateless API wrapper - no data is persisted. All information comes directly from Kaspi.kz APIs in real-time.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Open Source Community",
            Url = new Uri("https://github.com/your-repo/kaspi-api-wrapper")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add examples for common response types
    c.DescribeAllParametersInCamelCase();
    c.SchemaGeneratorOptions.SchemaIdSelector = type => type.FullName?.Replace("+", ".");

    // Add servers information
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "http://localhost:5147",
        Description = "Development Server"
    });

    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "https://localhost:7147",
        Description = "Development Server (HTTPS)"
    });
});

// Register HTTP clients with appropriate configuration
builder.Services.AddHttpClient("KaspiWrapper", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.ConnectionClose = false;
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Register KaspiScraper HTTP client with automatic decompression
builder.Services.AddHttpClient("KaspiScraper", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.ConnectionClose = false;
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 5
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient();

// Register scraper configurations
builder.Services.Configure<KaspiScraperConfig>(builder.Configuration.GetSection("Scrapers:Kaspi"));

// Register anti-detection services
builder.Services.AddScoped<HeaderGenerator>();
builder.Services.AddScoped<RequestThrottler>();
builder.Services.AddScoped<SessionManager>();
builder.Services.AddScoped<AntiDetectionManager>();

// Register KaspiHttpClient with factory
builder.Services.AddScoped(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var logger = provider.GetRequiredService<ILogger<KaspiHttpClient>>();
    var headerGenerator = provider.GetRequiredService<HeaderGenerator>();
    var requestThrottler = provider.GetRequiredService<RequestThrottler>();
    var sessionManager = provider.GetRequiredService<SessionManager>();
    return new KaspiHttpClient(httpClientFactory, logger, headerGenerator, requestThrottler, sessionManager);
});

// Register Kaspi API clients
builder.Services.AddScoped<KaspiNavigationApiClient>();
builder.Services.AddScoped<KaspiProductFilterApiClient>();
// Register KaspiProductDetailExtractor for extracting specifications and descriptions
builder.Services.AddScoped(provider =>
{
    var kaspiHttpClient = provider.GetRequiredService<KaspiHttpClient>();
    var logger = provider.GetRequiredService<ILogger<KaspiProductDetailExtractor>>();
    return new KaspiProductDetailExtractor(kaspiHttpClient, logger);
});
builder.Services.AddScoped(provider =>
{
    var kaspiHttpClient = provider.GetRequiredService<KaspiHttpClient>();
    var logger = provider.GetRequiredService<ILogger<KaspiMerchantProfileClient>>();
    return new KaspiMerchantProfileClient(kaspiHttpClient, logger);
});
builder.Services.AddScoped(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var logger = provider.GetRequiredService<ILogger<KaspiOfferApiClient>>();
    var kaspiHttpClient = provider.GetRequiredService<KaspiHttpClient>();
    return new KaspiOfferApiClient(httpClientFactory, logger, kaspiHttpClient);
});
builder.Services.AddScoped<KaspiApiCategoryScanner>();
builder.Services.AddScoped<CategoryValidationService>();

// Register location services
builder.Services.AddScoped<KaspiLocationService>();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(
                    "http://localhost:3000", 
                    "http://localhost:5173", 
                    "http://localhost:8080")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kaspi.kz API Wrapper v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Add request logging middleware
app.UseMiddleware<ApiRequestLoggingMiddleware>();

app.MapControllers();

app.Run();

/// <summary>
/// Entry point for the application, made public for integration testing
/// </summary>
public partial class Program { }
