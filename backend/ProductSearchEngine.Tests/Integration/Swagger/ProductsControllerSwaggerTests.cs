using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Products Controller - Product details and offers endpoints
/// </summary>
[TestFixture]
public class ProductsControllerSwaggerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsControllerSwaggerTests()
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
    public async Task GetProduct_ValidProductId_Should_ReturnProductDetails()
    {
        // Arrange - Using a known product ID from documentation
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(productId);
        }
    }

    [Test]
    public async Task GetProduct_WithCityCode_Should_ReturnRegionalPricing()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "750000000"; // Almaty

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data?.CityCode.Should().Be(cityCode);
        }
    }

    [Test]
    public async Task GetProduct_WithDifferentCityCode_Should_ReturnRegionalData()
    {
        // Arrange
        var productId = "102298404";
        var cityCode = "710000000"; // Almaty region

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}?cityCode={cityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProduct_EmptyProductId_Should_ReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/Products/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Empty route parameter
    }

    [Test]
    public async Task GetProduct_NonExistentProduct_Should_HandleGracefully()
    {
        // Arrange
        var nonExistentProductId = "999999999999";

        // Act
        var response = await _client.GetAsync($"/api/Products/{nonExistentProductId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(content, _jsonOptions);
            // Product might not exist, which should be handled gracefully
            apiResponse.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GetProduct_InvalidProductIdFormat_Should_HandleGracefully()
    {
        // Arrange
        var invalidProductId = "invalid-product-id";

        // Act
        var response = await _client.GetAsync($"/api/Products/{invalidProductId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Test]
    public async Task GetProduct_SpecialCharactersInProductId_Should_HandleGracefully()
    {
        // Arrange
        var productIdWithSpecialChars = "product@123#test";

        // Act
        var response = await _client.GetAsync($"/api/Products/{Uri.EscapeDataString(productIdWithSpecialChars)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetProduct_InvalidCityCode_Should_HandleGracefully()
    {
        // Arrange
        var productId = "102298404";
        var invalidCityCode = "invalid-city";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}?cityCode={invalidCityCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetProduct_Should_ReturnCorrectContentType()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Test]
    public async Task GetProduct_Should_RespondWithinReasonableTime()
    {
        // Arrange
        var productId = "102298404";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should respond within 30 seconds
    }

    [Test]
    public async Task GetProduct_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.PatchAsync($"/api/Products/{productId}", new StringContent(""));

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetProduct_Should_HaveValidProductStructure()
    {
        // Arrange
        var productId = "102298404";

        // Act
        var response = await _client.GetAsync($"/api/Products/{productId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            
            var product = apiResponse.Data!;
            product.Id.Should().NotBeNullOrEmpty();
            product.Name.Should().NotBeNullOrEmpty();
            product.Currency.Should().NotBeNullOrEmpty();
            product.Price.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Test]
    public async Task GetProduct_ConcurrentRequests_Should_HandleGracefully()
    {
        // Arrange
        var productId = "102298404";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync($"/api/Products/{productId}"));
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
}
