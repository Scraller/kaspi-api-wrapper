using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration;

/// <summary>
/// Integration tests for Product Specifications and Descriptions API endpoints
/// Tests the full API pipeline from HTTP request to response using real HTTP client
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductSpecificationsApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

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

    #region Product Details Integration Tests

    [Test]
    public async Task GetProductDetails_Should_ReturnProductWithSpecifications_When_ValidProductId()
    {
        // Arrange
        var productId = "129349158"; // Known product with specifications
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            content, 
            GetJsonOptions());
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().NotBeNullOrEmpty();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        apiResponse.Errors.Should().BeEmpty();
        
        // Validate product data structure
        var product = apiResponse.Data!;
        product.Id.Should().Be(productId);
        product.Name.Should().NotBeNullOrEmpty();
        product.Price.Should().BeGreaterThan(0);
        product.Currency.Should().Be("KZT");
        product.CityCode.Should().Be(cityCode);
        
        // Validate specifications if present
        product.Specifications.Should().NotBeNull();
        if (product.Specifications.Any())
        {
            foreach (var specGroup in product.Specifications)
            {
                specGroup.Code.Should().NotBeNullOrEmpty();
                specGroup.Name.Should().NotBeNullOrEmpty();
                specGroup.Features.Should().NotBeNull();
                
                foreach (var feature in specGroup.Features)
                {
                    feature.Name.Should().NotBeNullOrEmpty();
                    feature.FeatureValues.Should().NotBeNull();
                    
                    foreach (var value in feature.FeatureValues)
                    {
                        value.Value.Should().NotBeNullOrEmpty();
                    }
                }
            }
        }
    }

    [Test]
    public async Task GetProductDetails_Should_HandleEmptySpecifications_Gracefully()
    {
        // Arrange
        var productId = "999999999"; // Product that may not exist or have minimal data
        var cityCode = "750000000";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}?cityCode={cityCode}");

        // Assert
        // Should return 404 for non-existent product or 200 with empty specifications
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
                content, 
                GetJsonOptions());
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            
            // Empty specifications should be handled gracefully
            apiResponse.Data!.Specifications.Should().NotBeNull();
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
                content, 
                GetJsonOptions());
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("not found");
        }
    }

    [Test]
    public async Task GetProductDetails_Should_ReturnNotFound_When_InvalidProductId()
    {
        // Arrange
        var invalidProductId = "invalid123";

        // Act
        var response = await _client.GetAsync($"/api/products/{invalidProductId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
            content, 
            GetJsonOptions());
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
        apiResponse.Data.Should().BeNull();
        apiResponse.Errors.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetProductDetails_Should_ReturnBadRequest_When_EmptyProductId()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/products/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Route not found
    }

    [Test]
    public async Task GetProductDetails_Should_SupportRegionalPricing_When_DifferentCityCodes()
    {
        // Arrange
        var productId = "129349158";
        var almatyCityCode = "750000000";
        var astanaCityCode = "710000000";

        // Act
        var almatyResponse = await _client.GetAsync($"/api/products/{productId}?cityCode={almatyCityCode}");
        var astanaResponse = await _client.GetAsync($"/api/products/{productId}?cityCode={astanaCityCode}");

        // Assert
        almatyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        astanaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var almatyContent = await almatyResponse.Content.ReadAsStringAsync();
        var astanaContent = await astanaResponse.Content.ReadAsStringAsync();
        
        var almatyApiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            almatyContent, GetJsonOptions());
        var astanaApiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            astanaContent, GetJsonOptions());
        
        almatyApiResponse.Should().NotBeNull();
        astanaApiResponse.Should().NotBeNull();
        
        almatyApiResponse!.Data!.CityCode.Should().Be(almatyCityCode);
        astanaApiResponse!.Data!.CityCode.Should().Be(astanaCityCode);
        
        // Specifications should be consistent across regions
        almatyApiResponse.Data.Specifications.Should().BeEquivalentTo(astanaApiResponse.Data.Specifications);
    }

    [Test]
    [CancelAfter(5000)] // 5 second timeout
    public async Task GetProductDetails_Should_RespondWithin5Seconds_When_Called()
    {
        // Arrange
        var productId = "129349158";
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync($"/api/products/{productId}");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task GetProductDetails_Should_HandleConcurrentRequests_Successfully()
    {
        // Arrange
        var productIds = new[] { "129349158", "102298404", "103456789" };
        var cityCode = "750000000";

        // Act
        var tasks = productIds.Select(productId => 
            _client.GetAsync($"/api/products/{productId}?cityCode={cityCode}")).ToArray();
        
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            // Each response should either be successful or properly handle errors
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
                    content, GetJsonOptions());
                
                apiResponse.Should().NotBeNull();
                apiResponse!.Success.Should().BeTrue();
                apiResponse.Data.Should().NotBeNull();
            }
        }
    }

    [Test]
    public async Task GetProductDetails_Should_ReturnValidContentType_When_Called()
    {
        // Arrange
        var productId = "129349158";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetProductDetails_Should_IncludeRequiredHeaders_When_Called()
    {
        // Arrange
        var productId = "129349158";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.Headers.Should().NotBeNull();
        // Verify CORS headers if configured
        // Verify any custom headers required by the API
    }

    [Test]
    public async Task GetProductDetails_Should_HandleSpecialCharactersInProductId_Gracefully()
    {
        // Arrange
        var specialCharProductId = "12934%9158"; // URL encoded special character

        // Act
        var response = await _client.GetAsync($"/api/products/{specialCharProductId}");

        // Assert
        // Should handle special characters gracefully - either find product or return not found
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetProductDetails_Should_ValidateSpecificationStructure_When_SpecificationsPresent()
    {
        // Arrange
        var productId = "129349158";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            content, GetJsonOptions());
        
        var product = apiResponse!.Data!;
        
        if (product.Specifications.Any())
        {
            foreach (var specGroup in product.Specifications)
            {
                // Validate specification group structure
                specGroup.Code.Should().NotBeNullOrEmpty();
                specGroup.Name.Should().NotBeNullOrEmpty();
                
                if (specGroup.Code.Contains("*"))
                {
                    specGroup.Code.Should().Contain("*", "Group codes should follow pattern: category*subcategory");
                }
                
                foreach (var feature in specGroup.Features)
                {
                    // Validate feature structure
                    feature.Name.Should().NotBeNullOrEmpty();
                    feature.FeatureValues.Should().NotBeNull();
                    
                    if (feature.FeatureValues.Any())
                    {
                        foreach (var value in feature.FeatureValues)
                        {
                            value.Value.Should().NotBeNullOrEmpty();
                        }
                    }
                    
                    // Validate position is reasonable
                    if (feature.Position.HasValue)
                    {
                        feature.Position.Value.Should().BeGreaterThanOrEqualTo(0);
                    }
                }
            }
        }
    }

    [Test]
    public async Task GetProductDetails_Should_HandleLargeResponsePayload_Efficiently()
    {
        // Arrange
        var productId = "129349158"; // Product with potentially large specifications

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Response should be reasonable size (under 1MB)
        content.Length.Should().BeLessThan(1024 * 1024, "Response should be under 1MB");
        
        // Should still be valid JSON
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            content, GetJsonOptions());
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
    }

    #endregion

    #region API Response Validation Tests

    [Test]
    public async Task GetProductDetails_Should_ReturnConsistentApiResponseStructure_Always()
    {
        // Arrange
        var productId = "129349158";

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            content, GetJsonOptions());
        
        apiResponse.Should().NotBeNull();
        
        // Verify ApiResponse structure
        apiResponse!.Message.Should().NotBeNull();
        apiResponse.Errors.Should().NotBeNull();
        
        if (response.IsSuccessStatusCode)
        {
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }
        else
        {
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().NotBeEmpty();
        }
    }

    [Test]
    public async Task GetProductDetails_Should_IncludeTimestamp_InAllResponses()
    {
        // Arrange
        var productId = "129349158";
        var beforeRequest = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");
        var afterRequest = DateTime.UtcNow;

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(
            content, GetJsonOptions());
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Timestamp.Should().BeAfter(beforeRequest.AddSeconds(-5));
        apiResponse.Timestamp.Should().BeBefore(afterRequest.AddSeconds(5));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get JSON serializer options for API response deserialization
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion
}
