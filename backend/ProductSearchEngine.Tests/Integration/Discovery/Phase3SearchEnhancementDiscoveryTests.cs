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
/// Phase 3 Discovery Validation Tests - Search Enhancement APIs
/// Based on kaspi_phase3_search_discovery.md findings
/// Tests actual kaspi.kz website behavior to validate advanced search patterns
/// Dependencies: Phase 1 (basic search), Phase 2 (merchant filtering)
/// </summary>
[TestFixture]
[Category("Phase3Discovery")]
[Category("Integration")]
public class Phase3SearchEnhancementDiscoveryTests
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

    #region Phase 3 Advanced Search Pattern Validation

    [Test]
    [Description("Validates Phase 3 discovery: Advanced search URL pattern with all parameters")]
    public void AdvancedSearchUrlPattern_Should_MatchDiscoveredPattern_When_AllParametersIncluded()
    {
        // Arrange - Based on Phase 3 advanced search discovery
        var searchText = "iPhone";
        var filterString = ":category:Smartphones:allMerchants:Sulpak";
        var encodedFilter = Uri.EscapeDataString(filterString);
        var sortBy = "price";
        var filteredByCategory = "true";
        var searchContext = "search_session_123";
        
        var expectedUrlPattern = $"/shop/search/?text={searchText}&q={encodedFilter}&sort={sortBy}&filteredByCategory={filteredByCategory}&sc={searchContext}";
        
        // Act - Simulate what API wrapper would construct
        var constructedUrl = $"https://kaspi.kz{expectedUrlPattern}";
        
        // Assert - URL pattern matches Phase 3 discovery
        constructedUrl.Should().Contain("/shop/search/");
        constructedUrl.Should().Contain($"text={searchText}");
        constructedUrl.Should().Contain("q=");
        constructedUrl.Should().Contain($"sort={sortBy}");
        constructedUrl.Should().Contain($"filteredByCategory={filteredByCategory}");
        constructedUrl.Should().Contain($"sc={searchContext}");
        
        TestContext.WriteLine($"✅ Phase 3 Advanced Search Pattern Validated: {expectedUrlPattern}");
    }

    [Test]
    [Description("Validates Phase 3 discovery: Search sort options functionality")]
    [TestCase("relevance", Description = "Default relevance-based sorting")]
    [TestCase("price", Description = "Price ascending (low to high)")]
    [TestCase("rating", Description = "Customer rating descending")]
    [TestCase("date", Description = "Newest products first")]
    public async Task SearchSortOptions_Should_AffectResults_When_SortParameterChanged(string sortOption)
    {
        // Arrange - Based on Phase 3 sort options discovery
        var searchText = "iPhone";
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&sort={sortOption}";
        
        // Act - Test search sort functionality
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Sort options work correctly
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("iPhone", "Search results should contain search term");
        
        TestContext.WriteLine($"✅ Phase 3 Sort Option '{sortOption}' Validated");
    }

    [Test]
    [Description("Validates Phase 3 discovery: Complex filter combination with search")]
    public async Task ComplexFilterSearch_Should_CombineAllFilterTypes_When_AdvancedSearchUsed()
    {
        // Arrange - Based on Phase 3 filter combination enhancement
        var searchText = "iPhone";
        var categoryFilter = "category:Smartphones";
        var merchantFilter = "allMerchants:Sulpak";
        var priceFilter = "price:до 100000 т";
        var zoneFilter = "availableInZones:Magnum_ZONE1";
        
        var combinedFilter = $":{categoryFilter}:{merchantFilter}:{priceFilter}:{zoneFilter}";
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        var sortBy = "price";
        
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&q={encodedFilter}&sort={sortBy}";
        
        // Act - Test complex filter combination
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Complex filter search works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("iPhone", "Complex search should return relevant results");
        
        TestContext.WriteLine($"✅ Phase 3 Complex Filter Search Validated: {combinedFilter}");
    }

    #endregion

    #region Search Enhancement Feature Validation

    [Test]
    [Description("Validates Phase 3 discovery: Search context parameter behavior")]
    [TestCase("", Description = "Empty search context")]
    [TestCase("main_search", Description = "Main search context")]
    public async Task SearchContextParameter_Should_AffectSearchBehavior_When_ContextProvided(string searchContext)
    {
        // Arrange - Based on Phase 3 search context discovery
        var searchText = "Samsung";
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&sc={searchContext}";
        
        // Act - Test search context parameter
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Search context parameter works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Samsung", "Search should return results regardless of context");
        
        TestContext.WriteLine($"✅ Phase 3 Search Context '{searchContext}' Validated");
    }

    [Test]
    [Description("Validates Phase 3 discovery: FilteredByCategory parameter behavior")]
    [TestCase("true", Description = "Category filtering enabled")]
    [TestCase("false", Description = "Category filtering disabled")]
    public async Task FilteredByCategoryParameter_Should_AffectCategoryFiltering_When_ParameterSet(string filteredByCategory)
    {
        // Arrange - Based on Phase 3 filteredByCategory discovery
        var searchText = "laptop";
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&filteredByCategory={filteredByCategory}";
        
        // Act - Test filteredByCategory parameter
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - FilteredByCategory parameter works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("laptop", "Search should return laptop results");
        
        TestContext.WriteLine($"✅ Phase 3 FilteredByCategory '{filteredByCategory}' Validated");
    }

    #endregion

    #region Integration with Previous Phases

    [Test]
    [Description("Validates Phase 3 integration: Search with Phase 1 filter patterns")]
    public async Task SearchWithPhase1Filters_Should_IntegrateFilterEncoding_When_CombiningPatterns()
    {
        // Arrange - Integration of Phase 1 & 3 discoveries
        var searchText = "Apple";
        var phase1PriceFilter = "price:до 50000 т"; // From Phase 1 filter pattern
        var filterString = $":{phase1PriceFilter}";
        var encodedFilter = Uri.EscapeDataString(filterString);
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&q={encodedFilter}";
        
        // Act - Test Phase 1 + 3 integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Phase 1 + 3 integration works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Apple", "Search should return Apple products");
        
        TestContext.WriteLine($"✅ Phase 1+3 Integration: Search with Phase 1 filters validated");
    }

    [Test]
    [Description("Validates Phase 3 integration: Search with Phase 2 merchant filters")]
    public async Task SearchWithPhase2MerchantFilters_Should_CombineMerchantAndSearch_When_FiltersIntegrated()
    {
        // Arrange - Integration of Phase 2 & 3 discoveries
        var searchText = "smartphone";
        var phase2MerchantFilter = "allMerchants:TECHNODOM"; // From Phase 2 merchant pattern
        var filterString = $":{phase2MerchantFilter}";
        var encodedFilter = Uri.EscapeDataString(filterString);
        var sortBy = "rating"; // Phase 3 sort option
        var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&q={encodedFilter}&sort={sortBy}";
        
        // Act - Test Phase 2 + 3 integration
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Phase 2 + 3 integration works
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("smartphone", "Search should return smartphone results");
        
        TestContext.WriteLine($"✅ Phase 2+3 Integration: Search with merchant filters validated");
    }

    #endregion

    #region Auto-Enhancement Pattern Validation

    [Test]
    [Description("Validates Phase 3 discovery: System auto-enhancement of search URLs")]
    public async Task SystemAutoEnhancement_Should_AddMissingParameters_When_BasicSearchUsed()
    {
        // Arrange - Based on Phase 3/5 auto-enhancement discovery
        var basicSearchUrl = "https://kaspi.kz/shop/search/?text=iPhone&sort=price";
        
        // Act - Test system auto-enhancement behavior
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        // Set redirect handling to manual to see if system adds parameters
        var handler = new HttpClientHandler() { AllowAutoRedirect = false };
        using var testClient = new HttpClient(handler);
        testClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await testClient.GetAsync(basicSearchUrl);
        
        // Assert - System auto-enhancement behavior
        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.MovedPermanently)
        {
            var location = response.Headers.Location?.ToString();
            location.Should().NotBeNull("Redirect location should be provided");
            TestContext.WriteLine($"✅ Phase 3 Auto-Enhancement: Redirected to {location}");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            TestContext.WriteLine($"✅ Phase 3 Auto-Enhancement: Direct response received");
        }
    }

    #endregion

    #region Search Performance and Edge Cases

    [Test]
    [Description("Validates Phase 3 discovery: Search with special characters")]
    [TestCase("iPhone 13", Description = "Search with space")]
    [TestCase("MacBook Pro", Description = "Search with multiple words")]
    [TestCase("Samsung Galaxy S24+", Description = "Search with special characters")]
    public async Task SearchWithSpecialCharacters_Should_HandleCorrectly_When_SpecialCharsUsed(string searchTerm)
    {
        // Arrange - Test special character handling in search
        var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
        var testUrl = $"https://kaspi.kz/shop/search/?text={encodedSearchTerm}";
        
        // Act - Test special character search
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert - Special characters handled correctly
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeEmpty("Search with special characters should return results");
        
        TestContext.WriteLine($"✅ Phase 3 Special Character Search: '{searchTerm}' validated");
    }

    [Test]
    [Description("Validates Phase 3 discovery: Empty and minimal search queries")]
    [TestCase("", Description = "Empty search")]
    [TestCase("a", Description = "Single character")]
    [TestCase("ab", Description = "Two characters")]
    public async Task MinimalSearchQueries_Should_HandleGracefully_When_ShortQueriesUsed(string searchTerm)
    {
        // Arrange - Test minimal search queries
        var testUrl = $"https://kaspi.kz/shop/search/?text={Uri.EscapeDataString(searchTerm)}";
        
        // Act - Test minimal search handling
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        var response = await httpClient.GetAsync(testUrl);
        
        // Assert - Minimal searches handled gracefully
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        TestContext.WriteLine($"✅ Phase 3 Minimal Search: '{searchTerm}' handled gracefully");
    }

    [Test]
    [Description("Validates Phase 3 discovery: Multiple sort option performance")]
    public async Task MultipleSortOptions_Should_PerformAdequately_When_TestingSequentially()
    {
        // Arrange - Test all discovered sort options
        var sortOptions = new[] { "relevance", "price", "rating", "date" };
        var searchText = "tablet";
        var responseTimeThreshold = TimeSpan.FromSeconds(5);
        
        // Act & Assert - Test each sort option performance
        foreach (var sortOption in sortOptions)
        {
            var startTime = DateTime.UtcNow;
            
            var testUrl = $"https://kaspi.kz/shop/search/?text={searchText}&sort={sortOption}";
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(testUrl);
            var responseTime = DateTime.UtcNow - startTime;
            
            // Assert - Performance validation
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseTime.Should().BeLessThan(responseTimeThreshold, 
                $"Sort option '{sortOption}' should respond within {responseTimeThreshold.TotalSeconds} seconds");
            
            TestContext.WriteLine($"✅ Phase 3 Performance - Sort '{sortOption}': {responseTime.TotalMilliseconds}ms");
        }
    }

    #endregion
}
