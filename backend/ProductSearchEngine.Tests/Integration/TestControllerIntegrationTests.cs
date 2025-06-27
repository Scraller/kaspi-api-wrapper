using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Tests.Integration;

/// <summary>
/// Integration tests for the Test Controller - Health Check endpoints
/// </summary>
[TestFixture]
public class TestControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public TestControllerIntegrationTests()
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
    public async Task GetHealth_Should_ReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/Test/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Kaspi.kz API Wrapper is healthy and running");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task GetHealth_Should_ReturnConsistentApiResponseStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/Test/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
        
        // Validate ApiResponse structure
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().NotBeNullOrEmpty();
        apiResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Errors.Should().NotBeNull();
    }

    [Test]
    public async Task GetHealth_Should_RespondQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/Test/health");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should respond within 5 seconds
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetHealth_Should_HandleMultipleConcurrentRequests()
    {
        // Arrange
        const int requestCount = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(_client.GetAsync("/api/Test/health"));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(requestCount);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // All requests within 10 seconds
    }

    [Test]
    public async Task GetHealth_InvalidHttpMethod_Should_ReturnMethodNotAllowed()
    {
        // Act
        var response = await _client.PostAsync("/api/Test/health", new StringContent(""));

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}
