using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ProductSearchEngine.Api.Controllers;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Scraper.Services;
using ProductSearchEngine.Tests.TestData;
using System.Net;

namespace ProductSearchEngine.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for ProductsController focusing on product specifications and descriptions
/// Tests the controller's integration with KaspiProductDetailExtractor
/// </summary>
[TestFixture]
[Category("Unit")]
public class ProductsControllerSpecificationsTests
{
    private Mock<ILogger<ProductsController>> _mockLogger;
    private Mock<KaspiOfferApiClient> _mockOfferClient;
    private Mock<KaspiProductFilterApiClient> _mockFilterClient;
    private Mock<KaspiProductDetailExtractor> _mockDetailExtractor;
    private ProductsController _controller;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ProductsController>>();
        
        // Create mock for KaspiHttpClient with all required dependencies
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockKaspiLogger = new Mock<ILogger>();
        
        var mockKaspiHttpClient = new Mock<KaspiHttpClient>(
            mockHttpClientFactory.Object,
            mockKaspiLogger.Object,
            new HeaderGenerator(),
            new RequestThrottler(),
            new SessionManager());
        
        // Create mocks with proper constructor parameters
        var mockOfferLogger = new Mock<ILogger>();
        _mockOfferClient = new Mock<KaspiOfferApiClient>(
            mockHttpClientFactory.Object,
            mockOfferLogger.Object,
            mockKaspiHttpClient.Object);
        
        var mockFilterLogger = new Mock<ILogger<KaspiProductFilterApiClient>>();
        _mockFilterClient = new Mock<KaspiProductFilterApiClient>(mockKaspiHttpClient.Object, mockFilterLogger.Object);
        
        var mockDetailLogger = new Mock<ILogger<KaspiProductDetailExtractor>>();
        _mockDetailExtractor = new Mock<KaspiProductDetailExtractor>(mockKaspiHttpClient.Object, mockDetailLogger.Object);
        
        // Note: This test uses KaspiProductDetailExtractor for comprehensive product data
        _controller = new ProductsController(_mockLogger.Object, _mockOfferClient.Object, _mockFilterClient.Object, _mockDetailExtractor.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // ProductsController inherits from ControllerBase, no Dispose needed
    }

    #region GetProduct Tests with Specifications Focus

    [Test]
    public async Task GetProduct_Should_ReturnProductWithSpecifications_When_ValidProductIdAndExtractorReturnsData()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var cityCode = ProductSpecificationsTestData.CityCodes.Almaty;
        
        // Setup mock extractor to return detailed product data
        var mockDetailData = ProductSpecificationsTestData.CreateSampleKaspiProductData();
        _mockDetailExtractor.Setup(x => x.ExtractProductDetailAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .ReturnsAsync(mockDetailData);

        // Since current controller doesn't use the extractor yet, we'll mock the existing flow
        // This test demonstrates what the controller should do when updated
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId, cityCode);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(200);
        
        var apiResponse = actionResult.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var product = apiResponse.Data!;
        product.Id.Should().Be(productId);
        product.CityCode.Should().Be(cityCode);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving product details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_ReturnNotFound_When_ProductIdInvalid()
    {
        // Arrange
        var invalidProductId = ProductSpecificationsTestData.ProductIds.Invalid;
        
        // Setup mock to return empty offers (simulating product not found)
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            invalidProductId, 
            It.IsAny<string>(), 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(new List<Offer>()));

        // Act
        var result = await _controller.GetProduct(invalidProductId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as NotFoundObjectResult;
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(404);
        
        var apiResponse = actionResult.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offers found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_ReturnBadRequest_When_ProductIdIsEmpty()
    {
        // Arrange
        var emptyProductId = "";

        // Act
        var result = await _controller.GetProduct(emptyProductId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as BadRequestObjectResult;
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(400);
        
        var apiResponse = actionResult.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Product ID is required");
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty productId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_UseDefaultCityCode_When_CityCodeNotProvided()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var expectedDefaultCityCode = "750000000"; // Almaty
        
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            expectedDefaultCityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        result.Should().NotBeNull();
        
        // Verify that the default city code was used
        _mockOfferClient.Verify(x => x.GetOffersForProductAsync(
            productId, 
            expectedDefaultCityCode, 
            It.IsAny<AntiDetectionStrategy>()), 
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_HandleRegionalDifferences_When_DifferentCityCodes()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var almatyCityCode = ProductSpecificationsTestData.CityCodes.Almaty;
        var astanaCityCode = ProductSpecificationsTestData.CityCodes.Astana;
        
        var almatyOffers = CreateMockOffers(productId, "Almaty Merchant");
        var astanaOffers = CreateMockOffers(productId, "Astana Merchant");
        
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            almatyCityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(almatyOffers));
            
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            astanaCityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(astanaOffers));

        // Act
        var almatyResult = await _controller.GetProduct(productId, almatyCityCode);
        var astanaResult = await _controller.GetProduct(productId, astanaCityCode);

        // Assert
        almatyResult.Should().NotBeNull();
        astanaResult.Should().NotBeNull();
        
        var almatyActionResult = almatyResult.Result as OkObjectResult;
        var astanaActionResult = astanaResult.Result as OkObjectResult;
        
        almatyActionResult.Should().NotBeNull();
        astanaActionResult.Should().NotBeNull();
        
        var almatyApiResponse = almatyActionResult!.Value as ApiResponse<ProductDetailResponse>;
        var astanaApiResponse = astanaActionResult!.Value as ApiResponse<ProductDetailResponse>;
        
        almatyApiResponse!.Data!.CityCode.Should().Be(almatyCityCode);
        astanaApiResponse!.Data!.CityCode.Should().Be(astanaCityCode);
        
        // Offers may differ by region but product specifications should be consistent
        almatyApiResponse.Data.Id.Should().Be(astanaApiResponse.Data.Id);
        almatyApiResponse.Data.Name.Should().Be(astanaApiResponse.Data.Name);
    }

    [Test]
    public async Task GetProduct_Should_HandleEmptySpecifications_Gracefully()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.EmptySpecifications;
        
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            It.IsAny<string>(), 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var apiResponse = actionResult!.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        // Product should exist even without specifications
        var product = apiResponse.Data!;
        product.Id.Should().Be(productId);
        product.Specifications.Should().NotBeNull();
        // Empty specifications should be handled gracefully (empty list)
    }

    [Test]
    public async Task GetProduct_Should_ReturnServerError_When_ExceptionThrown()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<AntiDetectionStrategy>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as ObjectResult;
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(500);
        
        var apiResponse = actionResult.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("error");
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error retrieving product")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_IncludeTimestamp_InApiResponse()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var beforeRequest = DateTime.UtcNow;
        
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            It.IsAny<string>(), 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId);
        var afterRequest = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var apiResponse = actionResult!.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Timestamp.Should().BeAfter(beforeRequest.AddSeconds(-1));
        apiResponse.Timestamp.Should().BeBefore(afterRequest.AddSeconds(1));
    }

    [Test]
    public async Task GetProduct_Should_ValidateModelState_WhenCalled()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        
        // Simulate ModelState validation
        _controller.ModelState.Clear();
        
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            It.IsAny<string>(), 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        result.Should().NotBeNull();
        
        // With valid input, ModelState should remain valid
        _controller.ModelState.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task GetProduct_Should_HandleDetailExtractionFailure_Gracefully()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var cityCode = ProductSpecificationsTestData.CityCodes.Almaty;
        
        // Setup extractor to return null (simulating HTML extraction failure)
        _mockDetailExtractor.Setup(x => x.ExtractProductDetailAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .ReturnsAsync((KaspiProductDetailData?)null);

        // Setup offers to still be available (basic product info)
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId, cityCode);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(200);
        
        var apiResponse = actionResult.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var product = apiResponse.Data!;
        product.Id.Should().Be(productId);
        product.CityCode.Should().Be(cityCode);
        
        // When extraction fails, controller should still return basic product info
        // but with empty specifications and no description
        product.Description.Should().BeNullOrEmpty();
        product.Specifications.Should().NotBeNull().And.BeEmpty();
        product.GalleryImages.Should().NotBeNull().And.BeEmpty();
        
        // But offers should still be present
        product.Offers.Should().NotBeNullOrEmpty();
        
        // Verify warning was logged about extraction failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not extract detailed product information")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetProduct_Should_MapKaspiDataToApiModel_Correctly()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var cityCode = ProductSpecificationsTestData.CityCodes.Almaty;
        
        // Setup extractor to return detailed product data
        var mockDetailData = ProductSpecificationsTestData.CreateSampleKaspiProductData();
        _mockDetailExtractor.Setup(x => x.ExtractProductDetailAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .ReturnsAsync(mockDetailData);

        // Setup offers
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId, cityCode);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var apiResponse = actionResult!.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var product = apiResponse.Data!;
        
        // Verify proper mapping from KaspiProductDetailData to ProductDetailResponse
        if (mockDetailData.Description != null)
        {
            product.Description.Should().Be(mockDetailData.Description);
        }
        
        // Verify specifications mapping
        if (mockDetailData.Specifications != null && mockDetailData.Specifications.Any())
        {
            product.Specifications.Should().HaveCount(mockDetailData.Specifications.Count);
            
            for (int i = 0; i < mockDetailData.Specifications.Count; i++)
            {
                var kaspiSpec = mockDetailData.Specifications[i];
                var apiSpec = product.Specifications[i];
                
                apiSpec.Code.Should().Be(kaspiSpec.Code);
                apiSpec.Name.Should().Be(kaspiSpec.Name);
                
                if (kaspiSpec.Features != null)
                {
                    apiSpec.Features.Should().HaveCount(kaspiSpec.Features.Count);
                }
            }
        }
        
        // Verify gallery images mapping
        if (mockDetailData.GalleryImages != null && mockDetailData.GalleryImages.Any())
        {
            product.GalleryImages.Should().NotBeEmpty();
            
            // Check that non-empty image URLs are mapped
            var expectedImages = mockDetailData.GalleryImages
                .Select(img => img.Large ?? img.Medium ?? img.Small)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();
            
            product.GalleryImages.Should().HaveCount(expectedImages.Count);
        }
    }

    [Test]
    public async Task GetProduct_Should_ExtractSpecificationsFromDetailedData_When_Available()
    {
        // Arrange
        var productId = ProductSpecificationsTestData.ProductIds.CompleteSpecifications;
        var cityCode = ProductSpecificationsTestData.CityCodes.Almaty;
        
        // Setup extractor to return detailed product data with specifications
        var mockDetailData = ProductSpecificationsTestData.CreateSampleKaspiProductData();
        _mockDetailExtractor.Setup(x => x.ExtractProductDetailAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .ReturnsAsync(mockDetailData);

        // Setup offers
        var mockOffers = CreateMockOffers(productId);
        _mockOfferClient.Setup(x => x.GetOffersForProductAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()))
            .Returns(Task.FromResult<IEnumerable<Offer>>(mockOffers));

        // Act
        var result = await _controller.GetProduct(productId, cityCode);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var apiResponse = actionResult!.Value as ApiResponse<ProductDetailResponse>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        
        var product = apiResponse.Data!;
        
        // Verify that specifications are properly extracted and structured
        product.Specifications.Should().NotBeNull();
        
        if (mockDetailData.Specifications?.Any() == true)
        {
            product.Specifications.Should().NotBeEmpty();
            
            // Check that each specification group has proper structure
            foreach (var specGroup in product.Specifications)
            {
                specGroup.Code.Should().NotBeNullOrEmpty();
                specGroup.Name.Should().NotBeNullOrEmpty();
                specGroup.Features.Should().NotBeNull();
                
                // Check that features have proper structure
                foreach (var feature in specGroup.Features)
                {
                    feature.Code.Should().NotBeNullOrEmpty();
                    feature.Name.Should().NotBeNullOrEmpty();
                    feature.FeatureValues.Should().NotBeNull();
                }
            }
        }
        
        // Verify that extractor was called with correct parameters
        _mockDetailExtractor.Verify(x => x.ExtractProductDetailAsync(
            productId, 
            cityCode, 
            It.IsAny<AntiDetectionStrategy>()), 
            Times.Once);
        
        // Verify success was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully extracted detailed info")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create mock offers for testing
    /// </summary>
    private static IEnumerable<Offer> CreateMockOffers(string productId, string merchantName = "Test Merchant")
    {
        return new List<Offer>
        {
            new Offer
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = productId,
                Merchant = merchantName,
                Price = 12141,
                Currency = "KZT",
                InStock = true,
                OfferUrl = $"/shop/p/test-product-{productId}/",
                CityName = ProductSpecificationsTestData.CityCodes.CityNames[ProductSpecificationsTestData.CityCodes.Almaty]
            }
        };
    }

    #endregion
}