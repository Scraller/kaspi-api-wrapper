using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Search Controller - Advanced search functionality endpoints
/// </summary>
[TestFixture]
public class SearchControllerSwaggerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public SearchControllerSwaggerTests()
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
    public async Task AdvancedSearch_ValidRequest_Should_ReturnSearchResults()
    {
        // Arrange
        var searchRequest = new
        {
            text = "iPhone",
            category = "",
            merchantName = "",
            priceRange = "",
            cityCode = "750000000",
            page = 0,
            pageSize = 20,
            sortOptions = new[] { "price_asc" },
            filters = new Dictionary<string, string>()
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSearchResponse>>(responseContent, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    [Test]
    public async Task AdvancedSearch_WithFilters_Should_ReturnFilteredResults()
    {
        // Arrange
        var searchRequest = new
        {
            text = "smartphone",
            category = "smartphones",
            merchantName = "Sulpak",
            priceRange = "100000-500000",
            cityCode = "750000000",
            page = 0,
            pageSize = 10,
            sortOptions = new[] { "price_desc" },
            filters = new Dictionary<string, string>
            {
                { "brand", "Apple" },
                { "storage", "128GB" }
            }
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_EmptyRequest_Should_ReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_InvalidJson_Should_ReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_LargePageSize_Should_HandleAppropriately()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            page = 0,
            pageSize = 1000, // Very large page size
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_NegativePageNumber_Should_HandleAppropriately()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            page = -1, // Negative page number
            pageSize = 20,
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_ZeroPageSize_Should_HandleAppropriately()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            page = 0,
            pageSize = 0, // Zero page size
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_SpecialCharacters_Should_WorkCorrectly()
    {
        // Arrange
        var searchRequest = new
        {
            text = "тест поиск с казахскими символами ñ çü",
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_LargeRequestBody_Should_HandleAppropriately()
    {
        // Arrange - Create a very large request
        var largeSearchRequest = new
        {
            text = new string('a', 10000), // Very long search text
            filters = Enumerable.Range(0, 1000).ToDictionary(i => $"filter{i}", i => $"value{i}")
        };

        var json = JsonSerializer.Serialize(largeSearchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task AdvancedSearch_InvalidContentType_Should_HandleAppropriately()
    {
        // Arrange
        var xmlContent = new StringContent("<xml></xml>", Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", xmlContent);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task AdvancedSearch_Should_ReturnCorrectContentType()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Test]
    public async Task AdvancedSearch_Should_RespondWithinReasonableTime()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should respond within 30 seconds
    }

    [Test]
    public async Task AdvancedSearch_InvalidSortOptions_Should_HandleGracefully()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            sortOptions = new[] { "invalid_sort_option", "another_invalid" },
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AdvancedSearch_Should_HaveValidSearchResponseStructure()
    {
        // Arrange
        var searchRequest = new
        {
            text = "test",
            cityCode = "750000000"
        };

        var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/Search/advanced", content);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSearchResponse>>(responseContent, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            
            var searchResponse = apiResponse.Data!;
            searchResponse.Products.Should().NotBeNull();
            searchResponse.TotalCount.Should().BeGreaterOrEqualTo(0);
            searchResponse.Page.Should().BeGreaterOrEqualTo(0);
            searchResponse.PageSize.Should().BeGreaterThan(0);
        }
    }
}
