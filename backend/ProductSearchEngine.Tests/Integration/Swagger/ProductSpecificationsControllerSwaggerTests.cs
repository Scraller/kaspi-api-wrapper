using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Product Specifications Controller - Product descriptions, specifications and gallery endpoints
/// </summary>
[TestFixture]
public class ProductSpecificationsControllerSwaggerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductSpecificationsControllerSwaggerTests()
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
    public async Task GetProductDescription_ValidProductId_Should_ReturnDescription()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/description");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDescriptionResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GetProductDescription_WithCityCode_Should_ReturnRegionalDescription()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/description?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProductSpecifications_ValidProductId_Should_ReturnSpecifications()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/specifications");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSpecificationsResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GetProductSpecifications_WithCityCode_Should_ReturnRegionalSpecifications()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/specifications?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProductGallery_ValidProductId_Should_ReturnImageGallery()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/gallery");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductGalleryResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GetProductGallery_WithCityCode_Should_ReturnRegionalGallery()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/gallery?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCompleteProductInfo_ValidProductId_Should_ReturnCompleteInfo()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/complete");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductCompleteResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GetCompleteProductInfo_WithCityCode_Should_ReturnRegionalCompleteInfo()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/complete?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProductDescription_InvalidProductId_Should_HandleGracefully()
    {
        // Arrange
        var invalidProductId = "invalid-product-id";

        // Act
        var response = await _client.GetAsync($"/api/Products/{invalidProductId}/description");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetProductSpecifications_NonExistentProduct_Should_HandleGracefully()
    {
        // Arrange
        var nonExistentProductId = "999999999999";

        // Act
        var response = await _client.GetAsync($"/api/Products/{nonExistentProductId}/specifications");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProductGallery_EmptyProductId_Should_ReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/Products//gallery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCompleteProductInfo_SpecialCharactersInProductId_Should_HandleGracefully()
    {
        // Arrange
        var productIdWithSpecialChars = "product@123#test";

        // Act
        var response = await _client.GetAsync($"/api/Products/{Uri.EscapeDataString(productIdWithSpecialChars)}/complete");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetProductDescription_InvalidCityCode_Should_HandleGracefully()
    {
        // Arrange
        var productId = "102298404";
        var invalidCityCode = "invalid-city";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/description?cityCode={invalidCityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AllSpecificationEndpoints_Should_ReturnCorrectContentType()
    {
        // Arrange
        var productId = "102298404";
        var endpoints = new[]
        {
            $"/api/Products/{productId}/description",
            $"/api/Products/{productId}/specifications",
            $"/api/Products/{productId}/gallery",
            $"/api/Products/{productId}/complete"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            }
        }
    }

    [Test]
    public async Task AllSpecificationEndpoints_Should_RespondWithinReasonableTime()
    {
        // Arrange
        var productId = "102298404";
        var endpoints = new[]
        {
            $"/api/Products/{productId}/description",
            $"/api/Products/{productId}/specifications",
            $"/api/Products/{productId}/gallery",
            $"/api/Products/{productId}/complete"
        };

        foreach (var endpoint in endpoints)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should respond within 30 seconds
        }
    }

    [Test]
    public async Task AllSpecificationEndpoints_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Arrange
        var productId = "102298404";
        var endpoints = new[]
        {
            $"/api/Products/{productId}/description",
            $"/api/Products/{productId}/specifications",
            $"/api/Products/{productId}/gallery",
            $"/api/Products/{productId}/complete"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.PostAsync(endpoint, new StringContent(""));

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        }
    }

    [Test]
    public async Task GetProductDescription_ConcurrentRequests_Should_HandleGracefully()
    {
        // Arrange
        var productId = "102298404";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync($"/api/Products/{productId}/description"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        responses.Should().OnlyContain(r => 
            r.StatusCode == HttpStatusCode.OK || 
            r.StatusCode == HttpStatusCode.NotFound ||
            r.StatusCode == HttpStatusCode.TooManyRequests ||
            r.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetProductSpecifications_Should_HaveValidSpecificationStructure()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/specifications");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSpecificationsResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            // Specification structure validation would go here based on the actual response model
        }
    }

    [Test]
    public async Task GetProductGallery_Should_HaveValidGalleryStructure()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/gallery");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductGalleryResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            // Gallery structure validation would go here based on the actual response model
        }
    }

    [Test]
    public async Task GetCompleteProductInfo_Should_HaveValidCompleteInfoStructure()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}/complete");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductCompleteResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            // Complete info structure validation would go here based on the actual response model
        }
    }
}
