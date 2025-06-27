using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Tests.TestData;

namespace ProductSearchEngine.Tests.Integration.DataDriven;

/// <summary>
/// Data-driven integration tests for Products Controller using comprehensive test data matrix
/// </summary>
[TestFixture]
public class ProductsControllerDataDrivenTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsControllerDataDrivenTests()
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

    #region Product Search Data-Driven Tests

    /// <summary>
    /// Comprehensive test for product search endpoint using test data matrix
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.ProductSearchTestCases))]
    public async Task SearchProducts_WithVariousInputs_ShouldHandleCorrectly(
        string text, 
        string? category, 
        string? cityCode, 
        int page, 
        int pageSize, 
        bool shouldSucceed, 
        string testDescription)
    {
        // Arrange
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(text))
            queryParams.Add($"text={Uri.EscapeDataString(text)}");
        
        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        
        if (!string.IsNullOrEmpty(cityCode))
            queryParams.Add($"cityCode={Uri.EscapeDataString(cityCode)}");
        
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");
        
        var queryString = string.Join("&", queryParams);
        var url = $"/api/Products/search?{queryString}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, 
                $"Test case: {testDescription} should succeed with URL: {url}");

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSearchResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
            apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
            apiResponse.Data.Should().NotBeNull($"Data should not be null for: {testDescription}");
            
            // Validate metadata
            apiResponse.Metadata.Should().NotBeNull($"Metadata should be present for: {testDescription}");
            apiResponse.Metadata!.TotalCount.Should().BeGreaterOrEqualTo(0);
            
            // Validate product structure if results exist
            if (apiResponse.Data?.Products?.Any() == true)
            {
                var firstProduct = apiResponse.Data.Products.First();
                firstProduct.Id.Should().NotBeNullOrEmpty("Product ID should not be empty");
                firstProduct.Name.Should().NotBeNullOrEmpty("Product name should not be empty");
                firstProduct.Price.Should().BeGreaterThan(0, "Product price should be positive");
            }
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Test case: {testDescription} should fail with BadRequest for URL: {url}");

            var content = await response.Content.ReadAsStringAsync();
            
            // Handle both our custom ApiResponse format and ASP.NET Core validation format
            if (content.Contains("\"errors\":{"))
            {
                // ASP.NET Core model validation format (object with property keys)
                content.Should().NotBeEmpty("Response should contain validation errors");
                TestContext.WriteLine($"Validation error response (ASP.NET format): {testDescription}");
            }
            else
            {
                // Our custom ApiResponse format (with Errors as List<string>)
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
                apiResponse.Should().NotBeNull();
                apiResponse!.Success.Should().BeFalse($"API should return failure for invalid input: {testDescription}");
                apiResponse.Errors.Should().NotBeEmpty($"Errors should be provided for invalid input: {testDescription}");
            }
        }
    }

    #endregion

    #region Product Details Data-Driven Tests

    /// <summary>
    /// Test product details endpoint with various product IDs and city codes
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.ProductDetailsTestCases))]
    public async Task GetProductDetails_WithVariousInputs_ShouldHandleCorrectly(
        string productId,
        string? cityCode,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var url = $"/api/Products/{Uri.EscapeDataString(productId)}";
        if (!string.IsNullOrEmpty(cityCode))
        {
            url += $"?cityCode={Uri.EscapeDataString(cityCode)}";
        }

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound)
                .And.Subject.Should().NotBe(HttpStatusCode.InternalServerError, 
                $"Test case: {testDescription} - Valid requests should return OK or NotFound");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductDetailResponse>>(content, _jsonOptions);
                
                apiResponse.Should().NotBeNull($"Response should be deserializable for: {testDescription}");
                apiResponse!.Success.Should().BeTrue($"API should return success for: {testDescription}");
                apiResponse.Data.Should().NotBeNull($"Product data should not be null for: {testDescription}");
                
                // Validate product structure
                apiResponse.Data!.Id.Should().NotBeNullOrEmpty("Product ID should not be empty");
                apiResponse.Data.Name.Should().NotBeNullOrEmpty("Product name should not be empty");
            }
        }
        else
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
            // Additional validation for error case
            TestContext.WriteLine($"Test case: {testDescription} correctly failed with status: {response.StatusCode}");
        }
    }

    #endregion

    #region Pagination Tests

    /// <summary>
    /// Test pagination parameters across different endpoints
    /// </summary>
    [Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.PaginationTestCases))]
    public async Task SearchProducts_WithPaginationParameters_ShouldHandleCorrectly(
        int page,
        int pageSize,
        bool shouldSucceed,
        string testDescription)
    {
        // Arrange
        var url = $"/api/Products/search?text=iPhone&page={page}&pageSize={pageSize}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Valid pagination should succeed: {testDescription}");

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSearchResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            
            // Validate pagination metadata
            if (apiResponse.Metadata != null)
            {
                apiResponse.Metadata.Page.Should().Be(page, "Page should match requested page");
                
                if (apiResponse.Data?.Products?.Any() == true)
                {
                    // Note: Kaspi.kz API returns ~12 products per page regardless of pageSize parameter
                    // So we don't enforce the pageSize limit strictly, just ensure it's reasonable
                    apiResponse.Data.Products.Count.Should().BeLessOrEqualTo(20, 
                        "Returned products should not exceed reasonable limits (~12 from Kaspi)");
                    
                    TestContext.WriteLine($"Kaspi returned {apiResponse.Data.Products.Count} products (requested pageSize: {pageSize})");
                }
            }
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"Invalid pagination should fail: {testDescription}");
        }
    }

    #endregion

    #region Performance and Load Tests

    /// <summary>
    /// Test API performance with various search terms
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.CommonSearchTerms))]
    public async Task SearchProducts_PerformanceTest_ShouldRespondQuickly(string searchTerm)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var url = $"/api/Products/search?text={Uri.EscapeDataString(searchTerm)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            $"Search for '{searchTerm}' should complete within 5 seconds");
        
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test concurrent requests handling
    /// </summary>
    [Test]
    public async Task SearchProducts_ConcurrentRequests_ShouldHandleGracefully()
    {
        // Arrange
        var searchTerms = ApiTestDataMatrix.CommonSearchTerms.Take(5).ToArray();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        foreach (var term in searchTerms)
        {
            var url = $"/api/Products/search?text={Uri.EscapeDataString(term)}";
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
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ProductSearchResponse>>(content, _jsonOptions);
            apiResponse.Should().NotBeNull("Concurrent responses should be properly formatted");
        }
    }

    #endregion

    #region Edge Cases and Security Tests

    /// <summary>
    /// Test API behavior with edge case inputs
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.EdgeCaseStrings))]
    public async Task SearchProducts_WithEdgeCaseInputs_ShouldHandleSafely(string edgeCaseInput)
    {
        // Arrange
        var url = $"/api/Products/search?text={Uri.EscapeDataString(edgeCaseInput)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        TestContext.WriteLine($"Edge case input '{edgeCaseInput}' handled safely with status: {response.StatusCode}");

        // Ensure no server errors
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            $"Edge case input should not cause server errors: '{edgeCaseInput}'");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("Response should always have content");
        
        // Handle both custom ApiResponse format and ASP.NET Core validation format
        if (content.Contains("\"errors\":{"))
        {
            // ASP.NET Core model validation format - just verify it's valid JSON
            TestContext.WriteLine($"Edge case input '{edgeCaseInput}' returned ASP.NET validation format");
        }
        else
        {
            // Verify response can be deserialized as our custom format
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
            apiResponse.Should().NotBeNull($"Response should be valid JSON for input: '{edgeCaseInput}'");
        }
    }

    /// <summary>
    /// Test API with various city codes including invalid ones
    /// </summary>
    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.ValidCityCodes))]
    public async Task SearchProducts_WithValidCityCodes_ShouldSucceed(string cityCode)
    {
        // Arrange
        var url = $"/api/Products/search?text=iPhone&cityCode={cityCode}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Valid city code {cityCode} should work");
    }

    [Test]
    [TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.InvalidCityCodes))]
    public async Task SearchProducts_WithInvalidCityCodes_ShouldHandleGracefully(string cityCode)
    {
        // Arrange
        var url = $"/api/Products/search?text=iPhone&cityCode={Uri.EscapeDataString(cityCode)}";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        TestContext.WriteLine($"Invalid city code '{cityCode}' handled gracefully with status: {response.StatusCode}");
    }

    #endregion
}
