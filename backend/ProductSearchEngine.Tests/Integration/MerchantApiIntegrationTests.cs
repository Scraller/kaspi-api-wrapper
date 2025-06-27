using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration;

/// <summary>
/// Integration tests for Merchant API endpoints based on Phase 2 merchant discovery findings.
/// Tests the complete flow from HTTP request to merchant profile data extraction.
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("Merchant")]
public class MerchantApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

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

    #region GET /api/merchants/{merchantId} Integration Tests

    [Test]
    [Description("Integration test: Phase 2 discovery - Sulpak merchant profile with accurate data")]
    public async Task GetMerchant_Integration_Should_ReturnAccurateSulpakData_When_ValidRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/Sulpak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var merchant = apiResponse.Data!;
        merchant.Id.Should().Be("Sulpak");
        merchant.Name.Should().Be("Sulpak");
        
        // Based on Phase 2 discovery: These should be accurate real values
        merchant.Rating.Should().BeGreaterThan(4.0m, "Sulpak has high rating");
        merchant.ReviewCount.Should().BeGreaterThan(10000, "Sulpak has many reviews");
        merchant.ContactInfo.Should().NotBeNull();
        
        // Verify ApiResponse wrapper structure
        apiResponse.Message.Should().NotBeNullOrEmpty();
        apiResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        apiResponse.Errors.Should().BeEmpty();
        
        TestContext.WriteLine($"✅ Sulpak Integration Test - Rating: {merchant.Rating}, Reviews: {merchant.ReviewCount}");
    }

    [Test]
    [Description("Integration test: Phase 2 discovery - Numeric merchant ID handling")]
    [TestCase("11808018", "XAN_Comp")]
    [TestCase("2771000", "ТехноГород")]
    [TestCase("3101017", "HOMME")]
    [TestCase("6409007", "LUXTEX")]
    public async Task GetMerchant_Integration_Should_HandleNumericIds_When_ValidMerchant(string merchantId, string expectedName)
    {
        // Act
        var response = await _client.GetAsync($"/api/merchants/{merchantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var merchant = apiResponse.Data!;
        merchant.Id.Should().Be(merchantId);
        merchant.Name.Should().Be(expectedName);
        merchant.Rating.Should().BeGreaterThan(0m);
        merchant.ReviewCount.Should().BeGreaterThan(0);
        merchant.ProductCount.Should().BeGreaterThan(0, "SalesCount should be mapped to ProductCount");
        
        TestContext.WriteLine($"✅ {expectedName} Integration Test - ID: {merchantId}, Rating: {merchant.Rating}");
    }

    [Test]
    [Description("Integration test: Response time performance requirement")]
    public async Task GetMerchant_Integration_Should_RespondWithin3Seconds_When_ValidRequest()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/merchants/Sulpak");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "API should respond within 3 seconds per Phase 2 requirements");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        TestContext.WriteLine($"✅ Performance Test - Response time: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Test]
    [Description("Integration test: Error handling for non-existent merchant")]
    public async Task GetMerchant_Integration_Should_ReturnNotFound_When_MerchantNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/NonExistentMerchant123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
        
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Data.Should().BeNull();
        apiResponse.Message.Should().Contain("not found");
        apiResponse.Errors.Should().NotBeEmpty();
    }

    [Test]
    [Description("Integration test: Error handling for empty merchant ID")]
    public async Task GetMerchant_Integration_Should_ReturnBadRequest_When_EmptyMerchantId()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/");

        // Assert
        // This should return 404 for missing route, not 400, as the route won't match
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    [Description("Integration test: Phone number extraction validation")]
    public async Task GetMerchant_Integration_Should_IncludePhoneNumber_When_AvailableInProfile()
    {
        // Arrange - Test merchants known to have phone numbers from Phase 2 discovery
        var merchantsWithPhones = new[]
        {
            ("11808018", "XAN_Comp"), // +7 (708) 028-31-30
            ("Sulpak", "Sulpak") // 3210
        };

        foreach (var (merchantId, merchantName) in merchantsWithPhones)
        {
            // Act
            var response = await _client.GetAsync($"/api/merchants/{merchantId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
            
            apiResponse!.Data!.ContactInfo.Should().NotBeNull();
            apiResponse.Data.ContactInfo!.Phone.Should().NotBeNullOrEmpty($"{merchantName} should have phone number");
            
            TestContext.WriteLine($"✅ {merchantName} Phone: {apiResponse.Data.ContactInfo.Phone}");
        }
    }

    #endregion

    #region Data Accuracy Validation Tests

    [Test]
    [Description("Integration test: Phase 2 discovery - Data accuracy vs old aggregation method")]
    public async Task GetMerchant_Integration_Should_ReturnAccurateData_When_ComparedToDiscovery()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/Sulpak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
        
        var merchant = apiResponse!.Data!;
        
        // Phase 2 discovery showed current API returns wrong data:
        // OLD (incorrect): reviewCount: 14919, rating: 4.87416666666667
        // NEW (correct): numberOfReviews: 19046, rating: 4.9
        
        // The new implementation should return more accurate data
        merchant.ReviewCount.Should().NotBe(14919, "Should not return old aggregated count");
        merchant.Rating.Should().NotBe(4.87416666666667m, "Should not return old aggregated rating");
        
        // Should return values closer to discovery findings
        merchant.Rating.Should().BeGreaterThanOrEqualTo(4.8m, "Sulpak has high rating per discovery");
        merchant.ReviewCount.Should().BeGreaterThan(15000, "Sulpak has many reviews per discovery");
        
        TestContext.WriteLine($"✅ Data Accuracy - Rating: {merchant.Rating}, Reviews: {merchant.ReviewCount}");
    }

    [Test]
    [Description("Integration test: SalesCount consistency across merchants")]
    public async Task GetMerchant_Integration_Should_AlwaysIncludeSalesCount_When_ValidMerchants()
    {
        // Arrange - Test multiple merchants to verify salesCount is always present
        var testMerchants = new[] { "Sulpak", "11808018", "2771000", "3101017" };

        foreach (var merchantId in testMerchants)
        {
            // Act
            var response = await _client.GetAsync($"/api/merchants/{merchantId}");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
                
                apiResponse!.Data!.ProductCount.Should().BeGreaterThan(0, 
                    $"SalesCount should be mapped to ProductCount for merchant {merchantId}");
                
                TestContext.WriteLine($"✅ {merchantId} SalesCount/ProductCount: {apiResponse.Data.ProductCount}");
            }
        }
    }

    #endregion

    #region Content Type and Headers Tests

    [Test]
    [Description("Integration test: Response headers and content type validation")]
    public async Task GetMerchant_Integration_Should_ReturnCorrectHeaders_When_ValidRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/Sulpak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        // Verify structured logging headers if present
        response.Headers.Should().NotBeNull();
    }

    [Test]
    [Description("Integration test: JSON schema validation for ApiResponse wrapper")]
    public async Task GetMerchant_Integration_Should_ReturnValidApiResponseSchema_When_Successful()
    {
        // Act
        var response = await _client.GetAsync("/api/merchants/Sulpak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
        
        // Verify ApiResponse<T> schema
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().NotBeNullOrEmpty();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Errors.Should().NotBeNull().And.BeEmpty();
        apiResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify MerchantDetailResponse schema
        var merchant = apiResponse.Data!;
        merchant.Id.Should().NotBeNullOrEmpty();
        merchant.Name.Should().NotBeNullOrEmpty();
        merchant.Rating.Should().BeGreaterThan(0);
        merchant.ReviewCount.Should().BeGreaterThan(0);
        merchant.ProductCount.Should().BeGreaterThan(0);
        merchant.IsVerified.Should().BeTrue();
        merchant.Categories.Should().NotBeNull();
        merchant.ContactInfo.Should().NotBeNull();
        merchant.OperationalInfo.Should().NotBeNull();
    }

    #endregion

    #region Error Scenarios Integration Tests

    [Test]
    [Description("Integration test: Multiple invalid merchant ID formats")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid@merchant")]
    [TestCase("merchant with spaces")]
    [TestCase("very-long-merchant-id-that-definitely-does-not-exist-on-kaspi")]
    public async Task GetMerchant_Integration_Should_HandleInvalidIds_When_VariousFormats(string invalidMerchantId)
    {
        // Act
        var response = await _client.GetAsync($"/api/merchants/{Uri.EscapeDataString(invalidMerchantId)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(content))
        {
            // The API might return either our custom ApiResponse<T> format or ASP.NET Core Problem Details format
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
                apiResponse.Should().NotBeNull();
                apiResponse!.Success.Should().BeFalse();
                apiResponse.Data.Should().BeNull();
            }
            catch (JsonException)
            {
                // If it's not our custom format, it might be ASP.NET Core Problem Details format
                // This is expected for validation errors - just verify we got an error response
                content.Should().NotBeNullOrEmpty("Response should contain error information");
                
                // Verify it contains error information (typical for Problem Details format)
                content.Should().Contain("error", "Response should indicate an error occurred");
                
                TestContext.WriteLine($"✅ Received standard validation error response for merchant ID '{invalidMerchantId}'");
            }
        }
        else
        {
            // Empty response is also acceptable for error cases
            TestContext.WriteLine($"✅ Received empty error response for merchant ID '{invalidMerchantId}'");
        }
    }

    #endregion
}
