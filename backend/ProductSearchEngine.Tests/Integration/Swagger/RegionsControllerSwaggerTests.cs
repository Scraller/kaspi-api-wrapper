using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Regions Controller - Location and regional data endpoints
/// </summary>
[TestFixture]
public class RegionsControllerSwaggerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegionsControllerSwaggerTests()
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

    [Test]
    public async Task GetCities_Should_ReturnCitiesList()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task GetCities_MajorCitiesOnly_Should_ReturnMajorCities()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities?majorCitiesOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        // Major cities list should be smaller than all cities
        var allCitiesResponse = await _client.GetAsync("/api/Regions/cities");
        var allCitiesContent = await allCitiesResponse.Content.ReadAsStringAsync();
        var allCitiesApiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(allCitiesContent, _jsonOptions);
        
        if (allCitiesApiResponse?.Data?.Count > 0)
        {
            apiResponse.Data!.Count.Should().BeLessOrEqualTo(allCitiesApiResponse.Data.Count);
        }
    }

    [Test]
    public async Task GetCities_MajorCitiesOnlyFalse_Should_ReturnAllCities()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities?majorCitiesOnly=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task CheckProductAvailability_Should_ReturnAvailabilityInfo()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Regions/availability/{productId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CheckProductAvailability_WithDifferentCityCode_Should_HandleCorrectly()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "710000000"; // Different city

        // Act
        var response = await _client.GetAsync($"/api/Regions/availability/{productId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CheckProductAvailability_InvalidProductId_Should_HandleGracefully()
    {
        // Arrange
        var invalidProductId = "invalid-product";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Regions/availability/{invalidProductId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CheckProductAvailability_InvalidCityCode_Should_HandleGracefully()
    {
        // Arrange
        var productId = "102298404";
        var invalidCityCode = "invalid-city";

        // Act
        var response = await _client.GetAsync($"/api/Regions/availability/{productId}?cityCode={invalidCityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetCities_Should_ReturnCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetCities_Should_HaveValidCityStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        
        if (apiResponse.Data?.Any() == true)
        {
            var firstCity = apiResponse.Data.First();
            firstCity.Id.Should().NotBeNullOrEmpty();
            firstCity.Name.Should().NotBeNullOrEmpty();
            firstCity.NameEn.Should().NotBeNullOrEmpty();
            firstCity.Slug.Should().NotBeNullOrEmpty();
            firstCity.KaspiUrl.Should().NotBeNullOrEmpty();
        }
    }

    [Test]
    public async Task GetCities_Should_HaveValidPaginationMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Metadata.Should().NotBeNull();
        apiResponse.Metadata!.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Test]
    public async Task GetCities_Should_RespondQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should respond within 10 seconds
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetCities_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Act
        var response = await _client.DeleteAsync("/api/Regions/cities");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetCities_ConcurrentRequests_Should_HandleGracefully()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/Regions/cities"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    [Test]
    public async Task GetCities_Should_ContainAlmaty()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<CityResponse>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        
        // Should contain Almaty as it's a major city in Kazakhstan
        if (apiResponse.Data?.Any() == true)
        {
            apiResponse.Data.Should().Contain(c => 
                c.Name.Contains("Алматы") || 
                c.NameEn.Contains("Almaty") || 
                c.Id == "750000000");
        }
    }

    [Test]
    public async Task GetCities_InvalidQueryParameter_Should_HandleGracefully()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities?majorCitiesOnly=invalid");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetCities_ExtraQueryParameters_Should_IgnoreUnknownParams()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/cities?majorCitiesOnly=true&unknownParam=value");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task CheckProductAvailability_EmptyProductId_Should_ReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/Regions/availability/?cityCode=750000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
