using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Tests.TestData;

namespace ProductSearchEngine.Tests.Integration.DataDriven;

/// <summary>
/// Data-driven integration tests for Regions Controller using comprehensive test data matrix
/// </summary>
[TestFixture]
public class RegionsControllerDataDrivenTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegionsControllerDataDrivenTests()
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

    #region Cities Endpoint Tests

    /// <summary>
    /// Test cities endpoint with various parameters
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.CitiesTestCases))]
    public async Task GetCities_WithVariousParameters_ShouldHandleCorrectly(
        bool? majorCitiesOnly,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var url = "/api/Regions/cities";
        if (majorCitiesOnly.HasValue)
        {
            url += $"?majorCitiesOnly={majorCitiesOnly.Value.ToString().ToLower()}";
        }

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            $"Cities endpoint should always succeed: {testDescription}");

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
        apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
        apiResponse.Data.Should().NotBeNull($"Cities data should not be null for: {testDescription}");
        apiResponse.Metadata.Should().NotBeNull($"Metadata should be present for: {testDescription}");
        
        // Validate city structure if results exist
        if (apiResponse.Data?.Any() == true)
        {
            var firstCity = apiResponse.Data.First();
            firstCity.Id.Should().NotBeNullOrEmpty("City ID should not be empty");
            firstCity.Name.Should().NotBeNullOrEmpty("City name should not be empty");
            firstCity.KaspiUrl.Should().NotBeNullOrEmpty("Kaspi URL should not be empty");
        }

        // If major cities only, should be subset of all cities
        if (majorCitiesOnly == true)
        {
            var allCitiesResponse = await _client.GetAsync("/api/Regions/cities?majorCitiesOnly=false");
            var allCitiesContent = await allCitiesResponse.Content.ReadAsStringAsync();
            var allCitiesApiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(allCitiesContent, _jsonOptions);
            
            if (allCitiesApiResponse?.Data?.Count > 0 && apiResponse.Data?.Count > 0)
            {
                apiResponse.Data.Count.Should().BeLessOrEqualTo(allCitiesApiResponse.Data.Count,
                    "Major cities should be subset of all cities");
            }
        }
    }

    #endregion

    #region City Search Tests

    /// <summary>
    /// Test city search endpoint with various search parameters
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.CitySearchTestCases))]
    public async Task SearchCities_WithVariousInputs_ShouldHandleCorrectly(
        string? query,
        bool majorOnly,
        int limit,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(query))
            queryParams.Add($"q={Uri.EscapeDataString(query)}");
        
        queryParams.Add($"majorOnly={majorOnly.ToString().ToLower()}");
        queryParams.Add($"limit={limit}");
        
        var queryString = string.Join("&", queryParams);
        var url = $"/api/Regions/cities/search?{queryString}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Valid city search should succeed: {testDescription}");

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
            apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
            apiResponse.Data.Should().NotBeNull($"Search results should not be null for: {testDescription}");
            apiResponse.Metadata.Should().NotBeNull($"Metadata should be present for: {testDescription}");
            
            // Validate limit is respected
            if (apiResponse.Data?.Count > 0)
            {
                apiResponse.Data.Count.Should().BeLessOrEqualTo(limit,
                    $"Results should not exceed limit for: {testDescription}");
            }
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            TestContext.WriteLine($"Test case: {testDescription} correctly failed with status: {response.StatusCode}");
        }
    }

    #endregion

    #region Product Availability Tests

    /// <summary>
    /// Test product availability endpoint with various inputs
    /// </summary>
    [Test]
    [TestCase("102298404", "750000000", true, "Valid product and city")]
    [TestCase("102298404", "710000000", true, "Valid product, different city")]
    [TestCase("102298404", "invalid-city", true, "Valid product, invalid city")]
    [TestCase("invalid-product", "750000000", false, "Invalid product ID")]
    [TestCase("", "750000000", false, "Empty product ID")]
    public async Task CheckProductAvailability_WithVariousInputs_ShouldHandleCorrectly(
        string productId,
        string cityCode,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var url = $"/api/Regions/availability/{Uri.EscapeDataString(productId)}?cityCode={Uri.EscapeDataString(cityCode)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            TestContext.WriteLine($"Availability check for {testDescription}: {response.StatusCode}");
        }
        else
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
            TestContext.WriteLine($"Test case: {testDescription} correctly failed with status: {response.StatusCode}");
        }
    }

    #endregion

    #region City Validation Tests

    /// <summary>
    /// Test individual city lookup with various city IDs
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.ValidCityCodes))]
    public async Task GetCity_WithValidCityIds_ShouldReturnCityDetails(string cityId)
    {
        // Arrange
        var url = $"/api/Regions/cities/{cityId}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        TestContext.WriteLine($"Valid city ID {cityId} handled with status: {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<CityResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull($"Response should be deserializable for city ID: {cityId}");
            apiResponse!.Success.Should().BeTrue($"API should return success for valid city ID: {cityId}");
            apiResponse.Data.Should().NotBeNull($"City data should not be null for: {cityId}");
            apiResponse.Data!.Id.Should().Be(cityId, "Returned city ID should match requested ID");
        }
    }

    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.InvalidCityCodes))]
    public async Task GetCity_WithInvalidCityIds_ShouldHandleGracefully(string cityId)
    {
        // Arrange
        var url = $"/api/Regions/cities/{Uri.EscapeDataString(cityId)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        TestContext.WriteLine($"Invalid city ID '{cityId}' handled with status: {response.StatusCode}");
    }

    /// <summary>
    /// Test that empty city ID correctly routes to base cities endpoint
    /// </summary>
    [Test]
    public async Task GetCity_WithEmptyCityId_ShouldReturnAllCities()
    {
        // Arrange
        var url = $"/api/Regions/cities/{Uri.EscapeDataString("")}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "Empty city ID should route to base cities endpoint and return all cities");
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull("Response should be deserializable");
        apiResponse!.Success.Should().BeTrue("API should return success");
        apiResponse.Data.Should().NotBeNull("Cities data should not be null");
        apiResponse.Data.Should().NotBeEmpty("Should return list of cities, not empty list");
        
        TestContext.WriteLine($"Empty city ID correctly returned {apiResponse.Data!.Count} cities");
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// Test cities endpoint performance
    /// </summary>
    [Test]
    public async Task GetCities_PerformanceTest_ShouldRespondQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, 
            "Cities endpoint should respond within 3 seconds");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Test concurrent city requests
    /// </summary>
    [Test]
    public async Task GetCities_ConcurrentRequests_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var endpoints = new[]
        {
            "/api/Regions/cities",
            "/api/Regions/cities?majorCitiesOnly=true",
            "/api/Regions/cities?majorCitiesOnly=false",
            "/api/Regions/cities/search?q=Алматы",
            "/api/Regions/cities/search?q=Нур&majorOnly=true"
        };

        // Act
        foreach (var endpoint in endpoints)
        {
            tasks.Add(_client.GetAsync(endpoint));
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
    /// Test cities search with special characters
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.EdgeCaseStrings))]
    public async Task SearchCities_WithEdgeCaseInputs_ShouldHandleSafely(string edgeCaseInput)
    {
        // Arrange
        var url = $"/api/Regions/cities/search?q={Uri.EscapeDataString(edgeCaseInput)}&limit=10";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        TestContext.WriteLine($"Edge case input '{edgeCaseInput}' handled with status: {response.StatusCode}");

        // Ensure no server errors
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            $"Edge case input should not cause server errors");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("Response should always have content");
    }

    #endregion
}
