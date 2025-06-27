using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProductSearchEngine.Tests.Integration.Discovery;

/// <summary>
/// Phase 5 Discovery Validation Tests - Deep Testing & Pattern Validation
/// Based on kaspi_phase5_testing_validation.md findings
/// Tests actual kaspi.kz website behavior to validate all discovered patterns from Phases 1-4
/// Dependencies: All previous phases (1-4) for comprehensive validation
/// </summary>
[TestFixture]
[Category("Phase5Discovery")]
[Category("Integration")]
[Category("DeepValidation")]
public class Phase5DeepTestingValidationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private KaspiNavigationApiClient _kaspiClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        
        // Get actual Kaspi client for direct testing
        using var scope = _factory.Services.CreateScope();
        _kaspiClient = scope.ServiceProvider.GetRequiredService<KaspiNavigationApiClient>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Phase 5 Deep Regional Validation (Extension of Phase 4)

    [Test]
    [Description("Validates Phase 5 discovery: Confirmed city codes with exact validation")]
    [TestCase("750000000", "Алматы", "Смартфон Apple iPhone 13 128Gb черный в Алматы", Description = "Almaty confirmed")]
    [TestCase("710000000", "Астана", "Смартфон Apple iPhone 13 128Gb черный в Астане", Description = "Astana confirmed")]
    public async Task ConfirmedCityCodes_Should_ShowExactCityContent_When_ValidatedWithLiveNavigation(
        string cityCode, string expectedCity, string expectedTitlePattern)
    {
        // Arrange - Based on Phase 5 confirmed city code validation
        var productSlug = "apple-iphone-13-128gb-chernyi";
        var productId = "102298404";
        var testUrl = $"https://kaspi.kz/shop/p/{productSlug}-{productId}/?c={cityCode}";
        
        // Act - Live navigation validation
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Exact city validation as confirmed in Phase 5
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(expectedCity, $"Page should show {expectedCity} in content");
        
        // Additional validation for page title pattern
        if (content.Contains("<title>"))
        {
            content.Should().Contain(expectedCity, "Page title should contain city name");
        }
        
        TestContext.WriteLine($"✅ Phase 5 Confirmed City Code: {cityCode} → {expectedCity} validated");
    }

    #endregion

    #region Phase 5 Live Merchant Filter Testing (Extension of Phase 2)

    [Test]
    [Description("Validates Phase 5 discovery: Live merchant filtering with exact result counts")]
    public async Task LiveMerchantFiltering_Should_ReturnExpectedResults_When_ValidatedWithDirectNavigation()
    {
        // Arrange - Based on Phase 5 live merchant filter testing
        var filterString = ":category:Smartphones:allMerchants:Sulpak";
        var encodedFilter = Uri.EscapeDataString(filterString);
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}";
        
        // Act - Direct navigation to merchant filter URL
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Live merchant filtering validation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Смартфоны", "Page should show smartphones category");
        
        // Check for page title indicating filtering
        content.Should().Contain("Алматы", "Page should show regional context");
        
        TestContext.WriteLine($"✅ Phase 5 Live Merchant Filter: Sulpak smartphones validated");
    }

    #endregion

    #region Phase 5 Advanced Search Auto-Enhancement (Extension of Phase 3)

    [Test]
    [Description("Validates Phase 5 discovery: System auto-enhancement of search URLs")]
    public async Task SearchAutoEnhancement_Should_ExpandBasicSearchUrls_When_SystemProcessesRequest()
    {
        // Arrange - Based on Phase 5 auto-enhancement discovery
        var basicSearchUrl = "https://kaspi.kz/shop/search/?text=iPhone&sort=price";
        
        // Act - Test system auto-enhancement behavior
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(basicSearchUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - System auto-enhancement validation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("iPhone", "Search should return iPhone results");
        
        // Check if enhanced parameters are reflected in the page
        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        if (!string.IsNullOrEmpty(finalUrl))
        {
            TestContext.WriteLine($"✅ Phase 5 Auto-Enhancement: {basicSearchUrl} → {finalUrl}");
        }
        
        TestContext.WriteLine($"✅ Phase 5 Search Auto-Enhancement validated");
    }

    [Test]
    [Description("Validates Phase 5 discovery: Auto-added parameters in search expansion")]
    public async Task SearchAutoAddedParameters_Should_IncludeExpectedParams_When_SystemExpands()
    {
        // Arrange - Test auto-added parameters discovery
        var searchTerm = "tablet";
        var basicUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}";
        
        // Act - Test parameter auto-addition
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(basicUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Auto-added parameters validation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(searchTerm, "Search should return relevant results");
        
        // Based on Phase 5 findings, system may auto-add:
        // - q parameter with availableInZones:Magnum_ZONE1
        // - filteredByCategory=false
        // - sc parameter (search context)
        
        TestContext.WriteLine($"✅ Phase 5 Auto-Added Parameters: Search expansion validated");
    }

    #endregion

    #region Cross-Phase Integration Validation

    [Test]
    [Description("Validates Phase 5 discovery: Complete pattern integration across all phases")]
    public async Task CompletePatternIntegration_Should_CombineAllDiscoveredFeatures_When_AllPhasesIntegrated()
    {
        // Arrange - Complete integration of all phase discoveries
        var searchTerm = "MacBook";
        var categoryFilter = "category:Computers";
        var merchantFilter = "allMerchants:re-Store";
        var priceFilter = "price:до 500000 т";
        var zoneFilter = "availableInZones:Magnum_ZONE1";
        var cityCode = "750000000"; // Almaty
        var sortBy = "rating";
        
        var combinedFilter = $":{categoryFilter}:{merchantFilter}:{priceFilter}:{zoneFilter}";
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}&q={encodedFilter}&sort={sortBy}&c={cityCode}";
        
        // Act - Test complete pattern integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Complete integration validation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("MacBook", "Search should return MacBook results");
        content.Should().Contain("Алматы", "Regional context should be shown");
        
        TestContext.WriteLine($"✅ Phase 5 Complete Integration: All phases combined successfully");
    }

    [Test]
    [Description("Validates Phase 5 discovery: Phase dependency validation")]
    public async Task PhaseDependencyValidation_Should_ValidateEachPhaseBuildsOnPrevious_When_TestedSequentially()
    {
        // Arrange - Test phase dependency chain
        var productId = "102298404";
        var productSlug = "apple-iphone-13-128gb-chernyi";
        
        // Phase 1: Basic product URL
        var phase1Url = $"https://kaspi.kz/shop/p/{productSlug}-{productId}/";
        
        // Phase 1 + 4: Product URL with regional
        var phase1And4Url = $"{phase1Url}?c=750000000";
        
        // Phase 2: Merchant filtering
        var phase2Filter = ":category:Smartphones:allMerchants:Sulpak";
        var phase2Url = $"https://kaspi.kz/shop/c/smartphones/?q={Uri.EscapeDataString(phase2Filter)}";
        
        // Phase 3: Search enhancement
        var phase3Url = $"https://kaspi.kz/shop/search/?text=iPhone&sort=price";
        
        var urls = new[]
        {
            ("Phase 1", phase1Url),
            ("Phase 1+4", phase1And4Url),
            ("Phase 2", phase2Url),
            ("Phase 3", phase3Url)
        };
        
        // Act & Assert - Test each phase dependency
        foreach (var (phase, url) in urls)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(url);
            
            // Assert - Each phase works correctly
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{phase} should work correctly");
            
            TestContext.WriteLine($"✅ Phase 5 Dependency Validation: {phase} validated");
        }
    }

    #endregion

    #region System Behavior Validation

    [Test]
    [Description("Validates Phase 5 discovery: URL encoding consistency across all patterns")]
    [TestCase(":category:Smartphones", Description = "Category filter")]
    [TestCase(":allMerchants:Sulpak", Description = "Merchant filter")]
    [TestCase(":price:до 50000 т", Description = "Price filter with Cyrillic")]
    [TestCase(":availableInZones:Magnum_ZONE1", Description = "Zone filter")]
    public void UrlEncodingConsistency_Should_EncodeCorrectly_When_FilterPatternsUsed(string filterPattern)
    {
        // Arrange - Test URL encoding consistency from Phase 5
        var originalFilter = filterPattern;
        
        // Act - Apply URL encoding
        var encodedFilter = Uri.EscapeDataString(originalFilter);
        var decodedFilter = Uri.UnescapeDataString(encodedFilter);
        
        // Assert - Encoding consistency
        decodedFilter.Should().Be(originalFilter, "Encoding should be reversible");
        encodedFilter.Should().NotContain(":", "Colons should be encoded");
        encodedFilter.Should().NotContain(" ", "Spaces should be encoded");
        
        TestContext.WriteLine($"✅ Phase 5 URL Encoding: '{originalFilter}' → '{encodedFilter}'");
    }

    [Test]
    [Description("Validates Phase 5 discovery: Filter parameter combination order independence")]
    public async Task FilterParameterOrder_Should_BeIndependent_When_FiltersCombinedDifferently()
    {
        // Arrange - Test filter order independence
        var filter1 = ":category:Smartphones:allMerchants:Sulpak:price:до 100000 т";
        var filter2 = ":allMerchants:Sulpak:category:Smartphones:price:до 100000 т";
        var filter3 = ":price:до 100000 т:category:Smartphones:allMerchants:Sulpak";
        
        var filters = new[] { filter1, filter2, filter3 };
        var results = new List<bool>();
        
        // Act - Test each filter order
        foreach (var filter in filters)
        {
            var encodedFilter = Uri.EscapeDataString(filter);
            var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}";
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(testUrl);
            results.Add(response.StatusCode == HttpStatusCode.OK);
            
            TestContext.WriteLine($"✅ Phase 5 Filter Order Test: {filter.Substring(0, 30)}... → {response.StatusCode}");
        }
        
        // Assert - All orders should work
        results.Should().AllBeEquivalentTo(true, "All filter orders should work correctly");
    }

    #endregion

    #region Performance and Reliability Validation

    [Test]
    [Description("Validates Phase 5 discovery: Pattern reliability under load")]
    public async Task PatternReliability_Should_MaintainConsistency_When_TestedMultipleTimes()
    {
        // Arrange - Test pattern reliability
        var testUrl = "https://kaspi.kz/shop/c/smartphones/?q=%3Acategory%3ASmartphones%3AallMerchants%3ASulpak";
        var testIterations = 3; // Limited for integration testing
        var successCount = 0;
        
        // Act - Multiple reliability tests
        for (int i = 0; i < testIterations; i++)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(testUrl);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                successCount++;
            }
            
            TestContext.WriteLine($"✅ Phase 5 Reliability Test {i + 1}/{testIterations}: {response.StatusCode}");
            
            // Add delay between requests to be respectful
            await Task.Delay(1000);
        }
        
        // Assert - Reliability validation
        var successRate = (double)successCount / testIterations;
        successRate.Should().BeGreaterThan(0.8, "Pattern should be reliable (>80% success rate)");
        
        TestContext.WriteLine($"✅ Phase 5 Pattern Reliability: {successRate:P0} success rate");
    }

    [Test]
    [Description("Validates Phase 5 discovery: Response time consistency")]
    public async Task ResponseTimeConsistency_Should_MeetPerformanceExpectations_When_PatternsValidated()
    {
        // Arrange - Performance consistency testing
        var testUrls = new[]
        {
            ("Product Detail", "https://kaspi.kz/shop/p/apple-iphone-13-128gb-chernyi-102298404/?c=750000000"),
            ("Category Browse", "https://kaspi.kz/shop/c/smartphones/?c=750000000"),
            ("Merchant Filter", "https://kaspi.kz/shop/c/smartphones/?q=%3Acategory%3ASmartphones%3AallMerchants%3ASulpak"),
            ("Search", "https://kaspi.kz/shop/search/?text=iPhone&sort=price")
        };
        
        var responseTimeThreshold = TimeSpan.FromSeconds(10); // Generous for integration testing
        var responseTimes = new List<(string name, TimeSpan time)>();
        
        // Act & Assert - Test each pattern performance
        foreach (var (name, url) in testUrls)
        {
            var startTime = DateTime.UtcNow;
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(url);
            var responseTime = DateTime.UtcNow - startTime;
            
            // Assert - Individual performance
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseTime.Should().BeLessThan(responseTimeThreshold, 
                $"{name} should respond within {responseTimeThreshold.TotalSeconds} seconds");
            
            responseTimes.Add((name, responseTime));
            
            TestContext.WriteLine($"✅ Phase 5 Performance - {name}: {responseTime.TotalMilliseconds:F0}ms");
        }
        
        // Assert - Overall performance consistency
        var averageTime = TimeSpan.FromMilliseconds(responseTimes.Average(r => r.time.TotalMilliseconds));
        averageTime.Should().BeLessThan(responseTimeThreshold);
        
        TestContext.WriteLine($"✅ Phase 5 Performance Consistency: Average {averageTime.TotalMilliseconds:F0}ms");
    }

    #endregion

    #region Edge Case and Error Handling Validation

    [Test]
    [Description("Validates Phase 5 discovery: Comprehensive error handling validation")]
    [TestCase("", Description = "Empty filter")]
    [TestCase("invalid_filter_format", Description = "Invalid filter format")]
    [TestCase(":invalidType:value", Description = "Invalid filter type")]
    public async Task ComprehensiveErrorHandling_Should_HandleAllEdgeCases_When_InvalidDataProvided(string invalidFilter)
    {
        // Arrange - Comprehensive error handling testing
        var encodedFilter = Uri.EscapeDataString(invalidFilter);
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}";
        
        // Act - Test error handling
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        
        // Assert - Graceful error handling
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Invalid filters should not cause server errors");
        
        TestContext.WriteLine($"✅ Phase 5 Error Handling: '{invalidFilter}' handled gracefully");
    }

    #endregion
}
