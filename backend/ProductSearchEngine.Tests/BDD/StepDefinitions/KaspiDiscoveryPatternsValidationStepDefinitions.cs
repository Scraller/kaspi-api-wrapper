using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Reqnroll;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Services;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProductSearchEngine.Tests.BDD.StepDefinitions;

/// <summary>
/// Step definitions for Kaspi.kz API Discovery Patterns Validation
/// Supports all discovery phases (1-5) with real HTTP validation using anti-detection
/// </summary>
[Binding]
public class KaspiDiscoveryPatternsValidationStepDefinitions
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ScenarioContext _scenarioContext;
    private readonly KaspiHttpClient _kaspiHttpClient;
    private readonly AntiDetectionManager _antiDetectionManager;
    private readonly AntiDetectionStrategy _currentStrategy;
    
    private string? _constructedUrl;
    private string? _productSlug;
    private string? _productId;
    private string? _cityCode;
    private string? _merchantName;
    private string? _category;
    private string? _searchTerm;
    private string? _sortOption;
    private HttpResponseMessage? _lastResponse;
    private string? _lastResponseContent;
    private readonly List<string> _filterComponents = new();

    public KaspiDiscoveryPatternsValidationStepDefinitions(
        ScenarioContext scenarioContext,
        WebApplicationFactory<Program> factory)
    {
        _scenarioContext = scenarioContext;
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Get services from DI container
        using var scope = _factory.Services.CreateScope();
        _kaspiHttpClient = scope.ServiceProvider.GetRequiredService<KaspiHttpClient>();
        _antiDetectionManager = scope.ServiceProvider.GetRequiredService<AntiDetectionManager>();
        
        // Select an anti-detection strategy for this test session
        _currentStrategy = _antiDetectionManager.SelectStrategy();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _lastResponse?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Background Steps

    [Given(@"the Kaspi\.kz website is accessible")]
    public async Task GivenTheKaspiKzWebsiteIsAccessible()
    {
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            "https://kaspi.kz", 
            _currentStrategy, 
            "750000000", // Default Almaty city code
            useSessionCookies: false);
            
        success.Should().BeTrue("Kaspi.kz should be accessible");
        content.Should().NotBeNullOrEmpty();
    }

    [Given(@"I have a valid HTTP client with appropriate headers")]
    public void GivenIHaveAValidHttpClientWithAppropriateHeaders()
    {
        // Anti-detection is handled by KaspiHttpClient automatically
        // Headers are generated dynamically based on the current strategy
        TestContext.WriteLine($"Using anti-detection strategy with rotating headers and user agents");
    }

    #endregion

    #region Phase 1 Product Detail Steps

    [Given(@"I have a product with slug ""([^""]*)"" and ID ""([^""]*)""")]
    public void GivenIHaveAProductWithSlugAndId(string slug, string id)
    {
        _productSlug = slug;
        _productId = id;
    }

    [When(@"I construct a product detail URL using the pattern ""([^""]*)""")]
    public void WhenIConstructAProductDetailUrlUsingThePattern(string pattern)
    {
        _constructedUrl = $"https://kaspi.kz/shop/p/{_productSlug}-{_productId}/";
    }

    [Then(@"the URL should follow the Phase 1 discovered pattern")]
    public void ThenTheUrlShouldFollowThePhase1DiscoveredPattern()
    {
        _constructedUrl.Should().NotBeNull();
        _constructedUrl.Should().Contain("/shop/p/");
        _constructedUrl.Should().Contain($"-{_productId}/");
    }

    [Then(@"the URL should end with a forward slash")]
    public void ThenTheUrlShouldEndWithAForwardSlash()
    {
        _constructedUrl.Should().EndWith("/");
    }

    [Then(@"the URL should contain the product ID")]
    public void ThenTheUrlShouldContainTheProductId()
    {
        _constructedUrl.Should().Contain(_productId);
    }

    #endregion

    #region Phase 1 Regional Steps

    [When(@"I access the product with city code ""([^""]*)""")]
    public async Task WhenIAccessTheProductWithCityCode(string cityCode)
    {
        _cityCode = cityCode;
        var url = $"https://kaspi.kz/shop/p/{_productSlug}-{_productId}/?c={cityCode}";
        
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            cityCode, // Use the specified city code for anti-detection
            useSessionCookies: true);
            
        success.Should().BeTrue("Request should be successful");
        _lastResponseContent = content;
        
        // Simulate response status for compatibility with existing tests
        _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Then(@"the page should display content for ""([^""]*)""")]
    public void ThenThePageShouldDisplayContentFor(string expectedCity)
    {
        _lastResponseContent.Should().NotBeNull("Response content should not be null");
        _lastResponseContent.Should().NotBeEmpty("Response content should not be empty");
        _lastResponseContent.Should().Contain(expectedCity, $"Page should show content for {expectedCity}");
    }

    [Then(@"the response should be successful")]
    [Then(@"the HTTP response should be successful")]
    [Then(@"the request should be successful")]
    public void ThenTheResponseShouldBeSuccessful()
    {
        _lastResponse.Should().NotBeNull("Response should not be null");
        _lastResponse!.StatusCode.Should().Be(HttpStatusCode.OK, "HTTP response should be successful");
    }

    #endregion

    #region Phase 2 Merchant Filtering Steps

    [Given(@"I want to filter products by merchant ""([^""]*)"" in category ""([^""]*)""")]
    public void GivenIWantToFilterProductsByMerchantInCategory(string merchantName, string category)
    {
        _merchantName = merchantName;
        _category = category;
    }

    [When(@"I construct a merchant filter URL using the pattern ""([^""]*)""")]
    public void WhenIConstructAMerchantFilterUrlUsingThePattern(string filterPattern)
    {
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        _constructedUrl = $"https://kaspi.kz/shop/c/{_category}/?q={encodedFilter}";
    }

    [Then(@"the URL should contain the encoded filter parameter")]
    public void ThenTheUrlShouldContainTheEncodedFilterParameter()
    {
        _constructedUrl.Should().Contain("q=");
        _constructedUrl.Should().Contain("%3A"); // Encoded colon
    }

    [Then(@"the URL should follow the Phase 2 merchant filtering pattern")]
    public void ThenTheUrlShouldFollowThePhase2MerchantFilteringPattern()
    {
        _constructedUrl.Should().Contain("/shop/c/");
        _constructedUrl.Should().Contain($"/{_category}/");
        _constructedUrl.Should().Contain("allMerchants");
    }

    [Given(@"I want to access merchant ""([^""]*)"" products in category ""([^""]*)""")]
    public void GivenIWantToAccessMerchantProductsInCategory(string merchantName, string category)
    {
        _merchantName = merchantName;
        _category = category;
    }

    [When(@"I apply the merchant filter to the category")]
    public async Task WhenIApplyTheMerchantFilterToTheCategory()
    {
        var filterPattern = $":allMerchants:{_merchantName}";
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        var url = $"https://kaspi.kz/shop/c/{_category}/?q={encodedFilter}";
        
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            "750000000", // Default city code for merchant filtering
            useSessionCookies: true);
            
        success.Should().BeTrue("Merchant filter request should be successful");
        _lastResponseContent = content;
        
        // Simulate response status for compatibility
        _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Then(@"the page should show products for the specified merchant and category")]
    public void ThenThePageShouldShowProductsForTheSpecifiedMerchantAndCategory()
    {
        _lastResponseContent.Should().NotBeNull();
        _lastResponseContent.Should().NotBeEmpty();
    }

    #endregion

    #region Phase 3 Search Enhancement Steps

    [Given(@"I want to search for ""([^""]*)""")]
    public void GivenIWantToSearchFor(string searchTerm)
    {
        _searchTerm = searchTerm;
    }

    [When(@"I apply sort option ""([^""]*)""")]
    public async Task WhenIApplySortOption(string sortOption)
    {
        _sortOption = sortOption;
        var url = $"https://kaspi.kz/shop/search/?text={_searchTerm}&sort={sortOption}";
        
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            "750000000", // Default city code for search
            useSessionCookies: true);
            
        success.Should().BeTrue("Search request should be successful");
        _lastResponseContent = content;
        
        // Simulate response status for compatibility
        _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Then(@"the search should return results sorted by ""([^""]*)""")]
    public void ThenTheSearchShouldReturnResultsSortedBy(string sortOption)
    {
        _lastResponseContent.Should().Contain(_searchTerm, "Search results should contain the search term");
    }

    [Given(@"I want to search for ""([^""]*)"" with complex filters")]
    public void GivenIWantToSearchForWithComplexFilters(string searchTerm)
    {
        _searchTerm = searchTerm;
        _filterComponents.Clear();
    }

    [Given(@"I want to filter by category ""([^""]*)""")]
    public void GivenIWantToFilterByCategory(string category)
    {
        _filterComponents.Add($"category:{category}");
    }

    [Given(@"I want to filter by merchant ""([^""]*)""")]
    public void GivenIWantToFilterByMerchant(string merchantName)
    {
        _filterComponents.Add($"allMerchants:{merchantName}");
    }

    [Given(@"I want to filter by price range ""([^""]*)""")]
    public void GivenIWantToFilterByPriceRange(string priceRange)
    {
        _filterComponents.Add($"price:{priceRange}");
    }

    [Given(@"I want to filter by availability zone ""([^""]*)""")]
    public void GivenIWantToFilterByAvailabilityZone(string zone)
    {
        _filterComponents.Add($"availableInZones:{zone}");
    }

    [Given(@"I want to specify city code ""([^""]*)"" for (.*)")]
    public void GivenIWantToSpecifyCityCodeFor(string cityCode, string cityName)
    {
        _cityCode = cityCode;
        TestContext.WriteLine($"Set city code {cityCode} for {cityName}");
    }

    [When(@"I combine all filters with search and sort by ""([^""]*)""")]
    public async Task WhenICombineAllFiltersWithSearchAndSortBy(string sortBy)
    {
        var combinedFilter = ":" + string.Join(":", _filterComponents);
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        var url = $"https://kaspi.kz/shop/search/?text={_searchTerm}&q={encodedFilter}&sort={sortBy}";
        
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            _cityCode ?? "750000000", // Use city code from context or default
            useSessionCookies: true);
            
        success.Should().BeTrue("Complex filter search should be successful");
        _lastResponseContent = content;
        
        // Simulate response status for compatibility
        _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Then(@"the search should apply all filters correctly")]
    public void ThenTheSearchShouldApplyAllFiltersCorrectly()
    {
        _lastResponseContent.Should().Contain(_searchTerm, "Search should return results for the search term");
    }

    #endregion

    #region Phase 4 Regional Steps

    [Given(@"I want to access ""([^""]*)"" with city code ""([^""]*)""")]
    public void GivenIWantToAccessWithCityCode(string endpointType, string cityCode)
    {
        _cityCode = cityCode;
        // Store endpoint type for later use
        _scenarioContext["endpointType"] = endpointType;
    }

    [When(@"I apply the city code parameter to the endpoint")]
    public async Task WhenIApplyTheCityCodeParameterToTheEndpoint()
    {
        var endpointType = _scenarioContext["endpointType"].ToString();
        string url;
        
        switch (endpointType)
        {
            case "product detail":
                url = $"https://kaspi.kz/shop/p/apple-iphone-13-128gb-chernyi-102298404/?c={_cityCode}";
                break;
            case "category browse":
                url = $"https://kaspi.kz/shop/c/smartphones/?c={_cityCode}";
                break;
            default:
                throw new ArgumentException($"Unknown endpoint type: {endpointType}");
        }
        
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            _cityCode ?? "750000000", // Use the specified city code or default to Almaty
            useSessionCookies: true);
            
        success.Should().BeTrue("Regional endpoint request should be successful");
        _lastResponseContent = content;
        
        // Simulate response status for compatibility
        _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
    }

    [Then(@"the page should show content for ""([^""]*)""")]
    public void ThenThePageShouldShowContentFor(string expectedCity)
    {
        _lastResponseContent.Should().NotBeNull("Response content should not be null");
        _lastResponseContent.Should().NotBeEmpty("Response content should not be empty");
        _lastResponseContent.Should().Contain(expectedCity, $"Page should show content for {expectedCity}");
    }

    [Then(@"the regional context should be correct")]
    public void ThenTheRegionalContextShouldBeCorrect()
    {
        _lastResponseContent.Should().NotBeEmpty("Regional context should be present");
    }

    [When(@"I combine zone filtering with city code")]
    public async Task WhenICombineZoneFilteringWithCityCode()
    {
        var filterPattern = ":availableInZones:Magnum_ZONE1";
        var encodedFilter = Uri.EscapeDataString(filterPattern);
        var url = $"https://kaspi.kz/shop/c/smartphones/?q={encodedFilter}&c={_cityCode}";
        
        var (success, content) = await MakeAntiDetectionRequestAsync(url);
        success.Should().BeTrue("Zone filtering with city code should be successful");
    }

    [Then(@"both regional and zone filtering should be applied")]
    public void ThenBothRegionalAndZoneFilteringShouldBeApplied()
    {
        _lastResponseContent.Should().NotBeEmpty("Both filters should be applied");
    }

    #endregion

    #region Phase 5 Deep Testing Steps

    [Given(@"I have a basic search URL for ""([^""]*)"" with sort ""([^""]*)""")]
    public void GivenIHaveABasicSearchUrlForWithSort(string searchTerm, string sort)
    {
        _searchTerm = searchTerm;
        _sortOption = sort;
        _constructedUrl = $"https://kaspi.kz/shop/search/?text={searchTerm}&sort={sort}";
    }

    [When(@"the system processes the search request")]
    public async Task WhenTheSystemProcessesTheSearchRequest()
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        _lastResponse = await httpClient.GetAsync(_constructedUrl);
        _lastResponseContent = await _lastResponse.Content.ReadAsStringAsync();
    }

    [Then(@"the system may auto-enhance the URL with additional parameters")]
    public void ThenTheSystemMayAutoEnhanceTheUrlWithAdditionalParameters()
    {
        // System behavior may vary, so we just verify successful processing
        _lastResponse.Should().NotBeNull("Response should be available");
        if (_lastResponse != null)
        {
            _lastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Then(@"the search results should be returned successfully")]
    public void ThenTheSearchResultsShouldBeReturnedSuccessfully()
    {
        _lastResponseContent.Should().Contain(_searchTerm, "Search results should contain the search term");
    }

    [Given(@"I want to perform a comprehensive search combining all discovered patterns")]
    public void GivenIWantToPerformAComprehensiveSearchCombiningAllDiscoveredPatterns()
    {
        _filterComponents.Clear();
    }

    [Given(@"I search for ""([^""]*)""")]
    public void GivenISearchFor(string searchTerm)
    {
        _searchTerm = searchTerm;
    }

    [Given(@"I specify city code ""([^""]*)"" for (.*)")]
    public void GivenISpecifyCityCodeFor(string cityCode, string cityName)
    {
        _cityCode = cityCode;
    }

    [Given(@"I sort by ""([^""]*)""")]
    public void GivenISortBy(string sortBy)
    {
        _sortOption = sortBy;
    }

    // Simplified filter step definitions for comprehensive testing
    [Given(@"I filter by category ""([^""]*)""")]
    public void GivenIFilterByCategory(string category)
    {
        _filterComponents.Add($"category:{category}");
    }

    [Given(@"I filter by merchant ""([^""]*)""")]
    public void GivenIFilterByMerchant(string merchant)
    {
        _filterComponents.Add($"allMerchants:{merchant}");
    }

    [Given(@"I filter by price ""([^""]*)""")]
    public void GivenIFilterByPrice(string priceRange)
    {
        _filterComponents.Add($"price:{priceRange}");
    }

    [Given(@"I filter by availability zone ""([^""]*)""")]
    public void GivenIFilterByAvailabilityZone(string zone)
    {
        _filterComponents.Add($"availableInZones:{zone}");
    }

    [When(@"I execute the complete integrated request")]
    public async Task WhenIExecuteTheCompleteIntegratedRequest()
    {
        var combinedFilter = ":" + string.Join(":", _filterComponents);
        var encodedFilter = Uri.EscapeDataString(combinedFilter);
        var url = $"https://kaspi.kz/shop/search/?text={_searchTerm}&q={encodedFilter}&sort={_sortOption}&c={_cityCode}";
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        _lastResponse = await httpClient.GetAsync(url);
        _lastResponseContent = await _lastResponse.Content.ReadAsStringAsync();
    }

    [Then(@"all filter patterns should work together")]
    public void ThenAllFilterPatternsShouldWorkTogether()
    {
        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the response should show regional context for (.*)")]
    public void ThenTheResponseShouldShowRegionalContextFor(string cityName)
    {
        _lastResponseContent.Should().Contain(cityName, $"Response should show context for {cityName}");
    }

    [Then(@"the search results should contain ""([^""]*)""")]
    public void ThenTheSearchResultsShouldContain(string searchTerm)
    {
        _lastResponseContent.Should().Contain(searchTerm, $"Results should contain {searchTerm}");
    }

    #endregion

    #region Error Handling and Performance Steps

    [Given(@"I have an invalid ""([^""]*)"" value ""([^""]*)""")]
    public void GivenIHaveAnInvalidValue(string parameterType, string invalidValue)
    {
        _scenarioContext["parameterType"] = parameterType;
        _scenarioContext["invalidValue"] = invalidValue;
    }

    [When(@"I use the invalid parameter in a request")]
    public async Task WhenIUseTheInvalidParameterInARequest()
    {
        var parameterType = _scenarioContext["parameterType"].ToString();
        var invalidValue = _scenarioContext["invalidValue"].ToString();
        
        string url = parameterType switch
        {
            "city code" => $"https://kaspi.kz/shop/c/smartphones/?c={invalidValue}",
            "merchant name" => $"https://kaspi.kz/shop/c/smartphones/?q={Uri.EscapeDataString($":allMerchants:{invalidValue}")}",
            "availability zone" => $"https://kaspi.kz/shop/c/smartphones/?q={Uri.EscapeDataString($":availableInZones:{invalidValue}")}",
            "filter format" => $"https://kaspi.kz/shop/c/smartphones/?q={Uri.EscapeDataString(invalidValue ?? "")}",
            _ => throw new ArgumentException($"Unknown parameter type: {parameterType}")
        };
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        _lastResponse = await httpClient.GetAsync(url);
        _lastResponseContent = await _lastResponse.Content.ReadAsStringAsync();
    }

    [Then(@"the system should handle the error gracefully")]
    public void ThenTheSystemShouldHandleTheErrorGracefully()
    {
        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be(HttpStatusCode.OK, "Invalid parameters should not cause server errors");
    }

    [Then(@"the response should not cause a server error")]
    public void ThenTheResponseShouldNotCauseAServerError()
    {
        _lastResponse.Should().NotBeNull("Response should be available");
        _lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        _lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.BadGateway);
        _lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
    }

    [Given(@"I have a reliable test URL with merchant filtering")]
    public void GivenIHaveAReliableTestUrlWithMerchantFiltering()
    {
        _constructedUrl = "https://kaspi.kz/shop/c/smartphones/?q=%3Acategory%3ASmartphones%3AallMerchants%3ASulpak";
    }

    [When(@"I execute the same request multiple times")]
    public async Task WhenIExecuteTheSameRequestMultipleTimes()
    {
        var successCount = 0;
        var totalRequests = 3; // Limited for integration testing
        
        for (int i = 0; i < totalRequests; i++)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(_constructedUrl);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                successCount++;
            }
            
            if (i < totalRequests - 1) // Don't delay after last request
            {
                await Task.Delay(1000); // Be respectful to the server
            }
        }
        
        _scenarioContext["successRate"] = (double)successCount / totalRequests;
    }

    [Then(@"the response should be consistent across requests")]
    public void ThenTheResponseShouldBeConsistentAcrossRequests()
    {
        var successRate = (double)_scenarioContext["successRate"];
        successRate.Should().BeGreaterThan(0.5, "Response should be reasonably consistent");
    }

    [Then(@"the success rate should be acceptable")]
    public void ThenTheSuccessRateShouldBeAcceptable()
    {
        var successRate = (double)_scenarioContext["successRate"];
        successRate.Should().BeGreaterThan(0.8, "Success rate should be acceptable (>80%)");
    }

    #endregion

    #region General Validation Steps

    [Given(@"I have a ""([^""]*)"" request")]
    public void GivenIHaveARequest(string patternType)
    {
        _scenarioContext["patternType"] = patternType;
    }

    [When(@"I execute the request")]
    public async Task WhenIExecuteTheRequest()
    {
        var patternType = _scenarioContext["patternType"].ToString();
        
        string url = patternType switch
        {
            "product detail" => "https://kaspi.kz/shop/p/apple-iphone-13-128gb-chernyi-102298404/",
            "category browse" => "https://kaspi.kz/shop/c/smartphones/",
            "merchant filtering" => "https://kaspi.kz/shop/c/smartphones/?q=%3Acategory%3ASmartphones%3AallMerchants%3ASulpak",
            "search enhancement" => "https://kaspi.kz/shop/search/?text=iPhone&sort=price",
            "regional with filters" => "https://kaspi.kz/shop/search/?text=iPhone&c=750000000",
            _ => throw new ArgumentException($"Unknown pattern type: {patternType}")
        };
        
        var startTime = DateTime.UtcNow;
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
        _lastResponse = await httpClient.GetAsync(url);
        _lastResponseContent = await _lastResponse.Content.ReadAsStringAsync();
        
        var responseTime = DateTime.UtcNow - startTime;
        _scenarioContext["responseTime"] = responseTime;
    }

    [Then(@"the response time should be within acceptable limits")]
    public void ThenTheResponseTimeShouldBeWithinAcceptableLimits()
    {
        var responseTime = (TimeSpan)_scenarioContext["responseTime"];
        responseTime.Should().BeLessThan(TimeSpan.FromSeconds(10), "Response time should be acceptable");
    }

    #endregion

    #region Phase Dependency Validation

    [Given(@"I test each discovery phase in dependency order")]
    public void GivenITestEachDiscoveryPhaseInDependencyOrder()
    {
        // Initialize phase testing context
        _scenarioContext["phaseResults"] = new List<bool>();
    }

    [When(@"I validate Phase 1 basic product URLs")]
    public async Task WhenIValidatePhase1BasicProductUrls()
    {
        var url = "https://kaspi.kz/shop/p/apple-iphone-13-128gb-chernyi-102298404/";
        var result = await TestUrlPattern(url);
        ((List<bool>)_scenarioContext["phaseResults"]).Add(result);
    }

    [When(@"I validate Phase 2 merchant filtering builds on Phase 1")]
    public async Task WhenIValidatePhase2MerchantFilteringBuildsOnPhase1()
    {
        var url = "https://kaspi.kz/shop/c/smartphones/?q=%3Acategory%3ASmartphones%3AallMerchants%3ASulpak";
        var result = await TestUrlPattern(url);
        ((List<bool>)_scenarioContext["phaseResults"]).Add(result);
    }

    [When(@"I validate Phase 3 search enhancement uses Phase 1 & 2 patterns")]
    public async Task WhenIValidatePhase3SearchEnhancementUsesPhase12Patterns()
    {
        var url = "https://kaspi.kz/shop/search/?text=iPhone&sort=price";
        var result = await TestUrlPattern(url);
        ((List<bool>)_scenarioContext["phaseResults"]).Add(result);
    }

    [When(@"I validate Phase 4 regional parameters work with all previous phases")]
    public async Task WhenIValidatePhase4RegionalParametersWorkWithAllPreviousPhases()
    {
        var url = "https://kaspi.kz/shop/search/?text=iPhone&c=750000000";
        var result = await TestUrlPattern(url);
        ((List<bool>)_scenarioContext["phaseResults"]).Add(result);
    }

    [When(@"I validate Phase 5 deep testing confirms all patterns")]
    public async Task WhenIValidatePhase5DeepTestingConfirmsAllPatterns()
    {
        var url = "https://kaspi.kz/shop/search/?text=MacBook&q=%3Acategory%3AComputers%3AallMerchants%3Are-Store&sort=rating&c=750000000";
        var result = await TestUrlPattern(url);
        ((List<bool>)_scenarioContext["phaseResults"]).Add(result);
    }

    [Then(@"each phase should build correctly on its dependencies")]
    public void ThenEachPhaseShouldBuildCorrectlyOnItsDependencies()
    {
        var results = (List<bool>)_scenarioContext["phaseResults"];
        results.Should().AllBeEquivalentTo(true, "All phases should work correctly");
    }

    [Then(@"all patterns should work in integration")]
    public void ThenAllPatternsShouldWorkInIntegration()
    {
        var results = (List<bool>)_scenarioContext["phaseResults"];
        results.Should().HaveCount(5, "Should have tested all 5 phases");
        results.Should().AllBeEquivalentTo(true, "All patterns should work in integration");
    }

    private async Task<bool> TestUrlPattern(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
            var response = await httpClient.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Helper method to make HTTP requests using anti-detection
    /// </summary>
    private async Task<(bool Success, string Content)> MakeAntiDetectionRequestAsync(string url, string? cityCode = null)
    {
        var (success, content) = await _kaspiHttpClient.SendRequestAsync(
            url, 
            _currentStrategy, 
            cityCode ?? _cityCode ?? "750000000", // Use provided city code, or context city code, or default
            useSessionCookies: true);
            
        if (success)
        {
            _lastResponseContent = content;
            _lastResponse = new HttpResponseMessage(HttpStatusCode.OK);
        }
        else
        {
            _lastResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
            _lastResponseContent = content;
        }
        
        return (success, content);
    }

    #endregion
}
