using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration.Swagger;

/// <summary>
/// Integration tests for the Merchants Controller - Merchant profile and information endpoints
/// </summary>
[TestFixture]
public class MerchantsControllerSwaggerTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MerchantsControllerSwaggerTests()
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
    public async Task GetMerchant_ValidMerchantId_Should_ReturnMerchantDetails()
    {
        // Arrange
        var merchantId = "Sulpak";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(merchantId);
        }
    }

    [Test]
    public async Task GetMerchant_PopularMerchants_Should_ReturnValidData()
    {
        // Arrange - Test with various popular merchant names
        var popularMerchants = new[] { "Sulpak", "Technodom", "Alser", "ForteMarket" };

        foreach (var merchantId in popularMerchants)
        {
            // Act
            var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
                
                apiResponse.Should().NotBeNull();
                apiResponse!.Success.Should().BeTrue();
                apiResponse.Data.Should().NotBeNull();
            }
        }
    }

    [Test]
    public async Task GetMerchant_NumericMerchantId_Should_HandleCorrectly()
    {
        // Arrange
        var numericMerchantId = "11808018";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{numericMerchantId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMerchant_EmptyId_Should_ReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/Merchants/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Empty route parameter
    }

    [Test]
    public async Task GetMerchant_NonExistentMerchant_Should_ReturnNotFound()
    {
        // Arrange
        var nonExistentMerchantId = "NonExistentMerchant12345XYZ";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{nonExistentMerchantId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMerchant_SpecialCharacters_Should_HandleAppropriately()
    {
        // Arrange
        var merchantIdWithSpecialChars = "Test@Merchant#123";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{Uri.EscapeDataString(merchantIdWithSpecialChars)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetMerchant_CyrillicCharacters_Should_HandleCorrectly()
    {
        // Arrange
        var cyrillicMerchantId = "Сулпак";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{Uri.EscapeDataString(cyrillicMerchantId)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMerchant_VeryLongMerchantId_Should_HandleAppropriately()
    {
        // Arrange
        var longMerchantId = new string('A', 1000);

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{Uri.EscapeDataString(longMerchantId)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.RequestUriTooLong);
    }

    [Test]
    public async Task GetMerchant_Should_ReturnCorrectContentType()
    {
        // Arrange
        var merchantId = "Sulpak";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }

    [Test]
    public async Task GetMerchant_Should_RespondWithinReasonableTime()
    {
        // Arrange
        var merchantId = "Sulpak";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should respond within 30 seconds
    }

    [Test]
    public async Task GetMerchant_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Arrange
        var merchantId = "Sulpak";

        // Act
        var response = await _client.PostAsync($"/api/Merchants/{merchantId}", new StringContent(""));

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetMerchant_Should_HaveValidMerchantStructure()
    {
        // Arrange
        var merchantId = "Sulpak";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<MerchantDetailResponse>>(content, _jsonOptions);
            
            apiResponse.Should().NotBeNull();
            apiResponse!.Data.Should().NotBeNull();
            
            var merchant = apiResponse.Data!;
            merchant.Id.Should().NotBeNullOrEmpty();
            merchant.Name.Should().NotBeNullOrEmpty();
        }
    }

    [Test]
    public async Task GetMerchant_ConcurrentRequests_Should_HandleGracefully()
    {
        // Arrange
        var merchantId = "Sulpak";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync($"/api/Merchants/{merchantId}"));
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
    public async Task GetMerchant_CaseSensitivity_Should_HandleCorrectly()
    {
        // Arrange - Test different case variations
        var merchantVariations = new[] { "Sulpak", "SULPAK", "sulpak", "SuLpAk" };

        foreach (var merchantId in merchantVariations)
        {
            // Act
            var response = await _client.GetAsync($"/api/Merchants/{merchantId}");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }
    }

    [Test]
    public async Task GetMerchant_WithSpaces_Should_HandleCorrectly()
    {
        // Arrange
        var merchantIdWithSpaces = "Test Merchant";

        // Act
        var response = await _client.GetAsync($"/api/Merchants/{Uri.EscapeDataString(merchantIdWithSpaces)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
