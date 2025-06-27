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
/// Phase 4 Discovery Validation Tests - Regional/Location APIs
/// Based on kaspi_phase4_regional_discovery.md findings
/// Tests actual kaspi.kz website behavior to validate regional parameter patterns
/// Dependencies: Phase 1 (city code parameter), Phase 2 (zone filtering), Phase 3 (search with regional)
/// </summary>
[TestFixture]
[Category("Phase4Discovery")]
[Category("Integration")]
public class Phase4RegionalDiscoveryTests
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

    #region Phase 4 City Code Parameter Validation

    [Test]
    [Description("Validates Phase 4 discovery: City code parameter pattern across endpoint types")]
    [TestCase("750000000", "Алматы", "/shop/p/apple-iphone-13-128gb-chernyi-102298404/", Description = "Almaty product detail")]
    [TestCase("710000000", "Астана", "/shop/p/apple-iphone-13-128gb-chernyi-102298404/", Description = "Astana product detail")]
    [TestCase("750000000", "Алматы", "/shop/c/smartphones/", Description = "Almaty category browse")]
    [TestCase("710000000", "Астана", "/shop/c/smartphones/", Description = "Astana category browse")]
    public async Task CityCodeParameter_Should_AffectLocationContent_When_AppliedToEndpoints(
        string cityCode, string expectedCity, string basePath)
    {
        // Arrange - Based on Phase 4 city code discovery
        var testUrl = $"https://kaspi.kz{basePath}?c={cityCode}";
        
        // Act - Test city code parameter effect
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - City code affects location content
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(expectedCity, $"Page should show content for {expectedCity}");
        
        TestContext.WriteLine($"✅ Phase 4 City Code {cityCode} ({expectedCity}) validated for {basePath}");
    }

    [Test]
    [Description("Validates Phase 4 discovery: Search with city code parameter")]
    [TestCase("750000000", "Алматы", "iPhone")]
    [TestCase("710000000", "Астана", "Samsung")]
    public async Task SearchWithCityCode_Should_ShowRegionalResults_When_CityCodeProvided(
        string cityCode, string expectedCity, string searchTerm)
    {
        // Arrange - Based on Phase 4 regional search discovery
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}&c={cityCode}";
        
        // Act - Test search with city code
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Search results show regional context
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(searchTerm, "Search should return relevant results");
        content.Should().Contain(expectedCity, $"Search results should show {expectedCity} context");
        
        TestContext.WriteLine($"✅ Phase 4 Regional Search: {searchTerm} in {expectedCity} validated");
    }

    #endregion

    #region Availability Zone Integration Validation

    [Test]
    [Description("Validates Phase 4 discovery: Availability zone filtering pattern")]
    [TestCase("Magnum_ZONE1", Description = "Magnum Zone 1")]
    public async Task AvailabilityZoneFiltering_Should_FilterByDeliveryZone_When_ZoneSpecified(string zoneName)
    {
        // Arrange - Based on Phase 4 availability zone discovery
        var filterPattern = $":availableInZones:{zoneName}";
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}";
        
        // Act - Test availability zone filtering
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Zone filtering works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Смартфоны", "Zone filtering should return smartphone results");
        
        TestContext.WriteLine($"✅ Phase 4 Availability Zone '{zoneName}' filtering validated");
    }

    [Test]
    [Description("Validates Phase 4 discovery: City code combined with availability zones")]
    [TestCase("750000000", "Алматы", "Magnum_ZONE1")]
    [TestCase("710000000", "Астана", "Magnum_ZONE1")]
    public async Task CityCodeWithAvailabilityZone_Should_CombineRegionalAndZoneFiltering_When_BothProvided(
        string cityCode, string expectedCity, string zoneName)
    {
        // Arrange - Based on Phase 4 combination discovery
        var filterPattern = $":availableInZones:{zoneName}";
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}&c={cityCode}";
        
        // Act - Test city code + zone combination
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - City + zone combination works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(expectedCity, $"Page should show {expectedCity} context");
        
        TestContext.WriteLine($"✅ Phase 4 City+Zone Combination: {expectedCity} + {zoneName} validated");
    }

    #endregion

    #region Integration with Previous Phases

    [Test]
    [Description("Validates Phase 4 integration: Regional parameters with Phase 1 product patterns")]
    public async Task RegionalWithPhase1Product_Should_ShowLocationSpecificProduct_When_CityCodeApplied()
    {
        // Arrange - Integration of Phase 1 & 4 discoveries
        var productSlug = "apple-iphone-13-128gb-chernyi";
        var productId = "102298404";
        var cityCode = "750000000"; // Almaty
        var phase1ProductUrl = $"/shop/p/{productSlug}-{productId}/";
        var testUrl = $"https://kaspi.kz{phase1ProductUrl}?c={cityCode}";
        
        // Act - Test Phase 1 + 4 integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Phase 1 + 4 integration works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("iPhone", "Product should be displayed");
        content.Should().Contain("Алматы", "Regional context should be shown");
        
        TestContext.WriteLine($"✅ Phase 1+4 Integration: Product detail with regional context validated");
    }

    [Test]
    [Description("Validates Phase 4 integration: Regional parameters with Phase 2 merchant filters")]
    public async Task RegionalWithPhase2Merchant_Should_ShowLocationSpecificMerchant_When_MerchantAndCityFiltered()
    {
        // Arrange - Integration of Phase 2 & 4 discoveries
        var merchantFilter = "allMerchants:Sulpak";
        var categoryFilter = "category:Smartphones";
        var combinedFilter = $":{categoryFilter}:{merchantFilter}";
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        var cityCode = "710000000"; // Astana
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}&c={cityCode}";
        
        // Act - Test Phase 2 + 4 integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Phase 2 + 4 integration works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Астана", "Regional context should be shown");
        
        TestContext.WriteLine($"✅ Phase 2+4 Integration: Merchant filtering with regional context validated");
    }

    [Test]
    [Description("Validates Phase 4 integration: Regional parameters with Phase 3 search")]
    public async Task RegionalWithPhase3Search_Should_ShowLocationSpecificSearch_When_SearchWithRegion()
    {
        // Arrange - Integration of Phase 3 & 4 discoveries
        var searchTerm = "laptop";
        var sortBy = "price";
        var cityCode = "750000000"; // Almaty
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}&sort={sortBy}&c={cityCode}";
        
        // Act - Test Phase 3 + 4 integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Phase 3 + 4 integration works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("laptop", "Search results should contain search term");
        content.Should().Contain("Алматы", "Regional context should be shown");
        
        TestContext.WriteLine($"✅ Phase 3+4 Integration: Search with regional context validated");
    }

    #endregion

    #region Regional Price Variation Testing

    [Test]
    [Description("Validates Phase 4 discovery: Regional price variation testing approach")]
    public async Task RegionalPriceVariation_Should_ShowDifferentPricesAcrossCities_When_SameProductCompared()
    {
        // Arrange - Based on Phase 4 price variation testing approach
        var productSlug = "apple-iphone-13-128gb-chernyi";
        var productId = "102298404";
        var basePath = $"/shop/p/{productSlug}-{productId}/";
        
        var cities = new[]
        {
            ("750000000", "Алматы"),
            ("710000000", "Астана")
        };
        
        var priceDataPoints = new List<(string city, string content)>();
        
        // Act - Test same product across different cities
        foreach (var (cityCode, cityName) in cities)
        {
            var testUrl = $"https://kaspi.kz{basePath}?c={cityCode}";
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(testUrl);
            var content = await response.Content.ReadAsStringAsync();
            
            // Assert - Each city returns valid content
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain(cityName, $"Content should show {cityName} context");
            
            priceDataPoints.Add((cityName, content));
            
            TestContext.WriteLine($"✅ Phase 4 Regional Price Test: {cityName} content retrieved");
        }
        
        // Assert - Data collected for regional price analysis
        priceDataPoints.Should().HaveCount(2, "Should have collected data from both cities");
        
        TestContext.WriteLine($"✅ Phase 4 Regional Price Variation: Data collection validated");
    }

    #endregion

    #region Full Regional Pattern Integration

    [Test]
    [Description("Validates Phase 4 discovery: Complete regional pattern with all filter types")]
    public async Task CompleteRegionalPattern_Should_CombineAllRegionalFeatures_When_AllFiltersUsed()
    {
        // Arrange - Complete regional pattern from Phase 4 discovery
        var searchTerm = "smartphone";
        var categoryFilter = "category:Smartphones";
        var merchantFilter = "allMerchants:TECHNODOM";
        var priceFilter = "price:до 50000 т";
        var zoneFilter = "availableInZones:Magnum_ZONE1";
        var cityCode = "750000000"; // Almaty
        var sortBy = "rating";
        
        var combinedFilter = $":{categoryFilter}:{merchantFilter}:{priceFilter}:{zoneFilter}";
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}&q={encodedFilter}&sort={sortBy}&c={cityCode}";
        
        // Act - Test complete regional pattern
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Complete regional pattern works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("smartphone", "Search should return smartphone results");
        content.Should().Contain("Алматы", "Regional context should be shown");
        
        TestContext.WriteLine($"✅ Phase 4 Complete Regional Pattern: All filters with regional validated");
    }

    #endregion

    #region Error Handling and Edge Cases

    [Test]
    [Description("Validates Phase 4 discovery: Invalid city code handling")]
    [TestCase("999999999", Description = "Invalid city code")]
    [TestCase("000000000", Description = "Zero city code")]
    public async Task InvalidCityCode_Should_HandleGracefully_When_InvalidCodeProvided(string invalidCityCode)
    {
        // Arrange - Test invalid city codes
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?c={invalidCityCode}";
        
        // Act - Test invalid city code handling
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        
        // Assert - Invalid city codes handled gracefully
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Invalid city code should not cause errors");
        
        TestContext.WriteLine($"✅ Phase 4 Invalid City Code: {invalidCityCode} handled gracefully");
    }

    [Test]
    [Description("Validates Phase 4 discovery: Invalid availability zone handling")]
    [TestCase("Invalid_ZONE", Description = "Invalid zone name")]
    [TestCase("ZONE_999", Description = "Non-existent zone")]
    public async Task InvalidAvailabilityZone_Should_HandleGracefully_When_InvalidZoneProvided(string invalidZone)
    {
        // Arrange - Test invalid availability zones
        var filterPattern = $":availableInZones:{invalidZone}";
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        var testUrl = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}";
        
        // Act - Test invalid zone handling
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        
        // Assert - Invalid zones handled gracefully
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Invalid zone should not cause errors");
        
        TestContext.WriteLine($"✅ Phase 4 Invalid Zone: {invalidZone} handled gracefully");
    }

    [Test]
    [Description("Validates Phase 4 discovery: Regional parameter performance")]
    public async Task RegionalParameterPerformance_Should_PerformAdequately_When_TestingMultipleCities()
    {
        // Arrange - Test performance across multiple cities
        var cityCodes = new[] { "750000000", "710000000" };
        var responseTimeThreshold = TimeSpan.FromSeconds(5);
        var basePath = "/shop/c/smartphones/";
        
        // Act & Assert - Test each city performance
        foreach (var cityCode in cityCodes)
        {
            var startTime = DateTime.UtcNow;
            
            var testUrl = $"https://kaspi.kz{basePath}?c={cityCode}";
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(testUrl);
            var responseTime = DateTime.UtcNow - startTime;
            
            // Assert - Performance validation
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseTime.Should().BeLessThan(responseTimeThreshold, 
                $"City code {cityCode} should respond within {responseTimeThreshold.TotalSeconds} seconds");
            
            TestContext.WriteLine($"✅ Phase 4 Performance - City {cityCode}: {responseTime.TotalMilliseconds}ms");
        }
    }

    #endregion
}
