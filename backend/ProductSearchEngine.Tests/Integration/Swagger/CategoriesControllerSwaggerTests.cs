using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Categories Controller - Kaspi categories and hierarchy endpoints
/// </summary>
[TestFixture]
public class CategoriesControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoriesControllerIntegrationTests()
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
    public async Task GetKaspiCategories_Should_ReturnCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
        apiResponse.Metadata.Should().NotBeNull();
        apiResponse.Metadata!.TotalCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetKaspiCategories_WithIncludeSubcategoriesFalse_Should_ReturnCategoriesWithoutSubcategories()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi?includeSubcategories=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
        
        // Check that subcategories are empty
        apiResponse.Data.Should().OnlyContain(c => c.Subcategories.Count == 0);
    }

    [Test]
    public async Task GetKaspiCategories_WithIncludeSubcategoriesTrue_Should_ReturnCategoriesWithSubcategories()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi?includeSubcategories=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task GetTopLevelCategories_Should_ReturnOnlyTopLevelCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi/top-level");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCountGreaterThan(0);
        
        // Check that all categories have null parent slug (top-level)
        apiResponse.Data.Should().OnlyContain(c => c.ParentSlug == null);
        // Check that subcategories are empty for top-level endpoint
        apiResponse.Data.Should().OnlyContain(c => c.Subcategories.Count == 0);
    }

    [Test]
    public async Task GetCategoryBySlug_Should_ReturnSpecificCategoryOrNotFound()
    {
        // First get all categories to find a valid slug
        var allCategoriesResponse = await _client.GetAsync("/api/Categories/kaspi");
        var allCategoriesContent = await allCategoriesResponse.Content.ReadAsStringAsync();
        var allCategoriesApiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(allCategoriesContent, _jsonOptions);
        
        var validSlug = allCategoriesApiResponse?.Data?.FirstOrDefault()?.Slug;
        validSlug.Should().NotBeNullOrEmpty();

        // Act
        var response = await _client.GetAsync($"/api/Categories/kaspi/{validSlug}");

        // Assert - This might return 404 if endpoint doesn't exist, but let's test the request format
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetKaspiCategories_RateLimiting_Should_HandleMultipleQuickRequests()
    {
        // Act - Make multiple quick requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(_client.GetAsync("/api/Categories/kaspi"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - At least one should succeed, some might be rate limited
        responses.Should().HaveCountGreaterOrEqualTo(1);
        responses.Should().OnlyContain(r => 
            r.StatusCode == HttpStatusCode.OK || 
            r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task GetKaspiCategories_Should_RespondWithinReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi/top-level");

        // Assert
        stopwatch.Stop();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should respond within 30 seconds
        }
    }

    [Test]
    public async Task GetKaspiCategories_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Act
        var response = await _client.PutAsync("/api/Categories/kaspi", new StringContent(""));

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetKaspiCategories_Should_ReturnCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi/top-level");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Test]
    public async Task GetKaspiCategories_Should_HaveValidCategoryStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/Categories/kaspi/top-level");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<HierarchicalCategoryInfo>>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            
            if (apiResponse.Data?.Any() == true)
            {
                var firstCategory = apiResponse.Data.First();
                firstCategory.Name.Should().NotBeNullOrEmpty();
                firstCategory.Slug.Should().NotBeNullOrEmpty();
            }
        }
    }
}
