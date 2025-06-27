using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Tests.TestData;

namespace ProductSearchEngine.Tests.Integration.DataDriven;

/// <summary>
/// Data-driven integration tests for Search Controller using comprehensive test data matrix
/// </summary>
[TestFixture]
public class SearchControllerDataDrivenTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public SearchControllerDataDrivenTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Search Suggestions Tests

    /// <summary>
    /// Test search suggestions endpoint with various parameters
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.SearchSuggestionsTestCases))]
    public async Task GetSearchSuggestions_WithVariousInputs_ShouldHandleCorrectly(
        string query,
        int? limit,
        string? category,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(query))
            queryParams.Add($"query={Uri.EscapeDataString(query)}");
        
        if (limit.HasValue)
            queryParams.Add($"limit={limit.Value}");
        
        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        
        var queryString = string.Join("&", queryParams);
        var url = $"/api/Search/suggestions?{queryString}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Valid suggestions request should succeed: {testDescription}");

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
            apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
            apiResponse.Data.Should().NotBeNull($"Suggestions data should not be null for: {testDescription}");
            
            // Validate limit is respected
            if (limit.HasValue && apiResponse.Data?.Count > 0)
            {
                apiResponse.Data.Count.Should().BeLessOrEqualTo(limit.Value,
                    $"Results should not exceed limit for: {testDescription}");
            }

            // Validate suggestion structure if results exist
            if (apiResponse.Data?.Any() == true)
            {
                var firstSuggestion = apiResponse.Data.First();
                firstSuggestion.Text.Should().NotBeNullOrEmpty("Suggestion text should not be empty");
                
                // Suggestion should be related to the query
                if (!string.IsNullOrEmpty(query) && query.Length > 1)
                {
                    firstSuggestion.Text.ToLower().Should().Contain(query.ToLower(),
                        "Suggestion should be related to the query");
                }
            }
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    #endregion

    #region Search Filters Tests

    /// <summary>
    /// Test search filters endpoint with various category inputs
    /// </summary>
    [Test]
    [TestCase("phones", true, "Valid phone category")]
    [TestCase("computers", true, "Valid computer category")]
    [TestCase("electronics", true, "Valid electronics category")]
    [TestCase("", false, "Empty category")]
    [TestCase("invalid-category", true, "Invalid category (should handle gracefully)")]
    [TestCase(null, false, "Null category")]
    public async Task GetSearchFilters_WithVariousCategories_ShouldHandleCorrectly(
        string? category,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var url = "/api/Search/filters";
        if (!string.IsNullOrEmpty(category))
        {
            url += $"?category={Uri.EscapeDataString(category)}";
        }

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<SearchFiltersResponse>>(content, _jsonOptions);
                
                apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
                apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
                apiResponse.Data.Should().NotBeNull($"Filters data should not be null for: {testDescription}");
            }
        }
        else
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }
    }
    #endregion

    #region Performance Tests

    /// <summary>
    /// Test search suggestions performance with various query lengths
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.CommonSearchTerms))]
    public async Task GetSearchSuggestions_PerformanceTest_ShouldRespondQuickly(string searchTerm)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var url = $"/api/Search/suggestions?query={Uri.EscapeDataString(searchTerm)}&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
            $"Search suggestions for '{searchTerm}' should complete within 2 seconds");
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test concurrent search suggestion requests
    /// </summary>
    [Test]
    public async Task GetSearchSuggestions_ConcurrentRequests_ShouldHandleGracefully()
    {
        // Arrange
        var searchTerms = ApiTestDataMatrix.CommonSearchTerms.Take(5).ToArray();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        foreach (var term in searchTerms)
        {
            var url = $"/api/Search/suggestions?query={Uri.EscapeDataString(term)}&limit=5";
            tasks.Add(_client.GetAsync(url));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        responses.Should().OnlyContain(r => 
            r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.BadRequest);
        
        // Verify all responses are properly formatted
        foreach (var response in responses.Where(r => r.StatusCode == HttpStatusCode.OK))
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
            apiResponse.Should().NotBeNull("Concurrent responses should be properly formatted");
        }
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Test search suggestions with special characters and edge cases
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.EdgeCaseStrings))]
    public async Task GetSearchSuggestions_WithEdgeCaseInputs_ShouldHandleSafely(string edgeCaseInput)
    {
        // Arrange
        var url = $"/api/Search/suggestions?query={Uri.EscapeDataString(edgeCaseInput)}&limit=5";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        // Ensure no server errors
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "Edge case input should not cause server errors");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("Response should always have content");
        
        // Verify response can be deserialized - handle both formats
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            // Try to deserialize as ApiResponse first (custom validation errors)
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
                apiResponse.Should().NotBeNull($"Response should be valid ApiResponse JSON for input: '{edgeCaseInput}'");
                apiResponse!.Success.Should().BeFalse("BadRequest should have Success = false");
            }
            catch (JsonException)
            {
                // If that fails, try Problem Details format (ASP.NET Core model validation errors)
                var problemDetails = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                problemDetails.TryGetProperty("type", out _).Should().BeTrue("Problem Details should have 'type' property");
                problemDetails.TryGetProperty("title", out _).Should().BeTrue("Problem Details should have 'title' property");
                problemDetails.TryGetProperty("status", out _).Should().BeTrue("Problem Details should have 'status' property");
            }
        }
        else
        {
            // For OK responses, should be ApiResponse format
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
            apiResponse.Should().NotBeNull($"Response should be valid ApiResponse JSON for input: '{edgeCaseInput}'");
        }
    }

    /// <summary>
    /// Test search suggestions with various limit values
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.InvalidNumbers))]
    public async Task GetSearchSuggestions_WithInvalidLimits_ShouldHandleGracefully(int invalidLimit)
    {
        // Arrange
        var url = $"/api/Search/suggestions?query=iPhone&limit={invalidLimit}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
            
            // If the API accepts the invalid limit, it should handle it gracefully
            apiResponse.Should().NotBeNull();
            
            if (apiResponse?.Data != null)
            {
                // Should not return more than reasonable limit even with invalid input
                apiResponse.Data.Count.Should().BeLessOrEqualTo(100, 
                    "API should enforce reasonable upper limits even with invalid input");
            }
        }
    }

    #endregion

    #region Language and Encoding Tests

    /// <summary>
    /// Test search suggestions with different languages and encodings
    /// </summary>
    [Test]
    [TestCase("iPhone", "en", "English search term")]
    [TestCase("телефон", "ru", "Russian search term")]
    [TestCase("смартфон", "ru", "Russian smartphone term")]
    [TestCase("компьютер", "ru", "Russian computer term")]
    [TestCase("наушники", "ru", "Russian headphones term")]
    public async Task GetSearchSuggestions_WithDifferentLanguages_ShouldWork(
        string searchTerm,
        string language,
        string testDescription)
    {
        // Arrange
        var url = $"/api/Search/suggestions?query={Uri.EscapeDataString(searchTerm)}&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull($"Response should be deserializable for {language} text");
            
            if (apiResponse?.Data?.Any() == true)
            {
                // Suggestions should be relevant to the search term
                var firstSuggestion = apiResponse.Data.First();
                firstSuggestion.Text.Should().NotBeNullOrEmpty($"Suggestion should not be empty for {language}");
            }
        }
    }

    #endregion

    #region Search Quality Tests

    /// <summary>
    /// Test search suggestion relevance and quality
    /// </summary>
    [Test]
    [TestCase("iPhon", "Should suggest iPhone-related terms for partial spelling")]
    [TestCase("Sam", "Should suggest Samsung-related terms")]
    [TestCase("lap", "Should suggest laptop-related terms")]
    [TestCase("game", "Should suggest gaming-related terms")]
    public async Task GetSearchSuggestions_WithPartialTerms_ShouldProvideRelevantSuggestions(
        string partialTerm,
        string expectedBehavior)
    {
        // Arrange
        var url = $"/api/Search/suggestions?query={Uri.EscapeDataString(partialTerm)}&limit=20";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, expectedBehavior);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<SearchSuggestionResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        
        if (apiResponse?.Data?.Any() == true)
        {
            // At least some suggestions should start with or contain the partial term
            var relevantSuggestions = apiResponse.Data.Where(s => 
                s.Text.StartsWith(partialTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Text.Contains(partialTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            
            relevantSuggestions.Should().NotBeEmpty($"Should have relevant suggestions for '{partialTerm}'");
        }
    }

    #endregion
}
