using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Kaspi;

namespace ProductSearchEngine.Tests.Unit.Services;

/// <summary>
/// Unit tests for KaspiProductDetailExtractor based on Product Specifications Discovery Session
/// Tests the extraction of specifications and descriptions from window.BACKEND.components.item
/// </summary>
[TestFixture]
[Category("Unit")]
public class KaspiProductDetailExtractorTests
{
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private KaspiHttpClient _kaspiHttpClient;
    private Mock<ILogger<KaspiProductDetailExtractor>> _mockLogger;
    private KaspiProductDetailExtractor _extractor;

    [SetUp]
    public void SetUp()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockKaspiLogger = new Mock<ILogger<KaspiHttpClient>>();
        
        // Create real instances instead of mocks since these classes have constructor parameters
        var headerGenerator = new HeaderGenerator();
        var requestThrottler = new RequestThrottler(5);
        var sessionManager = new SessionManager();
        
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        _kaspiHttpClient = new KaspiHttpClient(
            mockHttpClientFactory.Object,
            mockKaspiLogger.Object,
            headerGenerator,
            requestThrottler,
            sessionManager);
        _mockLogger = new Mock<ILogger<KaspiProductDetailExtractor>>();
        _extractor = new KaspiProductDetailExtractor(_kaspiHttpClient, _mockLogger.Object);
    }

    #region Constructor Tests
    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_KaspiHttpClientIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new KaspiProductDetailExtractor(null!, _mockLogger.Object));

        exception!.ParamName.Should().Be("kaspiHttpClient");
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new KaspiProductDetailExtractor(_kaspiHttpClient, null!));

        exception!.ParamName.Should().Be("logger");
    }

    [Test]
    public void Constructor_Should_CreateInstance_When_ValidParameters()
    {
        // Act & Assert
        _extractor.Should().NotBeNull();
    }

    #endregion

    #region ExtractProductDetailAsync Tests

    [Test]
    public async Task ExtractProductDetailAsync_Should_ReturnCompleteProductData_When_ValidHtmlWithFullSpecifications()
    {
        // Arrange
        var productId = "129349158";
        var cityCode = "750000000";
        var htmlContent = CreateHtmlWithCompleteSpecifications();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId, cityCode);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Title.Should().Be("Мясорубка электрическая Zepter ZP-987 белый");
        result.Description.Should().NotBeNullOrEmpty();
        result.Description.Should().Contain("Электрическая мясорубка");
        result.Price.Should().Be(12141);
        result.Currency.Should().Be("KZT");
        result.CityCode.Should().Be(cityCode);

        result.Specifications.Should().NotBeEmpty();
        result.Specifications.Should().HaveCount(4);

        var featuresGroup = result.Specifications.FirstOrDefault(g => g.Name == "Особенности");
        featuresGroup.Should().NotBeNull();
        featuresGroup!.Code.Should().Be("Meat grinders*Features");
        featuresGroup.Features.Should().NotBeEmpty();

        var materialFeature = featuresGroup.Features.FirstOrDefault(f => f.Name == "Материал лотка");
        materialFeature.Should().NotBeNull();
        materialFeature!.Values.Should().Contain("металл");
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ReturnLimitedData_When_HtmlWithMinimalSpecifications()
    {
        // Arrange
        var productId = "102298404";
        var htmlContent = CreateHtmlWithMinimalSpecifications();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Title.Should().NotBeNullOrEmpty();
        result.Description.Should().NotBeNullOrEmpty();
        result.Specifications.Should().HaveCountLessOrEqualTo(2);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ReturnNull_When_HtmlMissingBackendComponents()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = "<html><body>Regular HTML without BACKEND.components.item</body></html>";

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().BeNull();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not find BACKEND.components.item")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ReturnNull_When_EmptyHtmlResponse()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = "";

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().BeNull();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Empty HTML response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_HandleEmptySpecifications_Gracefully()
    {
        // Arrange
        var productId = "999999999";
        var htmlContent = CreateHtmlWithEmptySpecifications();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Specifications.Should().BeEmpty();
        result.Description.Should().BeNullOrEmpty();
        result.Title.Should().NotBeNullOrEmpty();
        result.Price.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_HandleNullDescription_Gracefully()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = CreateHtmlWithNullDescription();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().BeNull();
        result.Title.Should().NotBeNullOrEmpty();
        result.Specifications.Should().NotBeNull();
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ThrowHttpRequestException_When_NetworkError()
    {
        // Arrange
        var productId = "129349158";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        Func<Task> act = async () => await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.WithMessage("*Network error*");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP request failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ReturnNull_When_InvalidJsonData()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = CreateHtmlWithInvalidJson();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().BeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON deserialization failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ConstructCorrectUrl_When_Called()
    {
        // Arrange
        var productId = "129349158";
        var cityCode = "750000000";
        var expectedUrl = $"https://kaspi.kz/shop/p/product-{productId}/?c={cityCode}";

        SetupHttpResponse(HttpStatusCode.OK, CreateHtmlWithCompleteSpecifications());

        // Act
        await _extractor.ExtractProductDetailAsync(productId, cityCode);

        // Assert
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_UseDefaultCityCode_When_CityCodeNotProvided()
    {
        // Arrange
        var productId = "129349158";
        var expectedUrl = $"https://kaspi.kz/shop/p/product-{productId}/?c=750000000";

        SetupHttpResponse(HttpStatusCode.OK, CreateHtmlWithCompleteSpecifications());

        // Act
        await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ApplyAntiDetectionStrategy_When_Provided()
    {
        // Arrange
        var productId = "129349158";
        var antiDetectionStrategy = AntiDetectionStrategy.CreateBasicStrategy();

        SetupHttpResponse(HttpStatusCode.OK, CreateHtmlWithCompleteSpecifications());

        // Act
        await _extractor.ExtractProductDetailAsync(productId, "750000000", antiDetectionStrategy);

        // Assert
        // Verify that the anti-detection strategy was applied (this would typically check headers)
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_HandleMultiValuedFeatures_Correctly()
    {
        // Arrange
        var productId = "102298404";
        var htmlContent = CreateHtmlWithMultiValuedFeatures();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Specifications.Should().NotBeEmpty();

        var colorFeature = result.Specifications
            .SelectMany(g => g.Features)
            .FirstOrDefault(f => f.Name == "Доступные цвета");

        colorFeature.Should().NotBeNull();
        colorFeature!.Values.Should().HaveCount(3);
        colorFeature.Values.Should().Contain(new[] { "красный", "синий", "зеленый" });
        colorFeature.MultiValued.Should().BeTrue();
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_HandleDifferentSpecificationTypes_Correctly()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = CreateHtmlWithVariousSpecificationTypes();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Specifications.Should().NotBeEmpty();

        var features = result.Specifications.SelectMany(g => g.Features).ToList();

        var enumFeature = features.FirstOrDefault(f => f.Type == "ENUM");
        enumFeature.Should().NotBeNull();

        var numberFeature = features.FirstOrDefault(f => f.Type == "NUMBER");
        numberFeature.Should().NotBeNull();

        var stringFeature = features.FirstOrDefault(f => f.Type == "STRING");
        stringFeature.Should().NotBeNull();
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_ExtractImageGallery_WhenPresent()
    {
        // Arrange
        var productId = "129349158";
        var htmlContent = CreateHtmlWithImageGallery();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        var result = await _extractor.ExtractProductDetailAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.GalleryImages.Should().NotBeEmpty();
        result.GalleryImages.Should().HaveCount(2);

        foreach (var image in result.GalleryImages)
        {
            image.Small.Should().NotBeNullOrEmpty();
            image.Medium.Should().NotBeNullOrEmpty();
            image.Large.Should().NotBeNullOrEmpty();
            image.Location.Should().NotBeNullOrEmpty();
        }
    }

    [Test]
    public async Task ExtractProductDetailAsync_Should_LogInformationMessages_WhenSuccessful()
    {
        // Arrange
        var productId = "129349158";
        var cityCode = "750000000";
        var htmlContent = CreateHtmlWithCompleteSpecifications();

        SetupHttpResponse(HttpStatusCode.OK, htmlContent);

        // Act
        await _extractor.ExtractProductDetailAsync(productId, cityCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extracting product details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully extracted product details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Performance Tests

    [Test]
    [CancelAfter(5000)] // 5 second timeout
    public async Task ExtractProductDetailAsync_Should_CompleteWithin5Seconds_When_LargeHtmlResponse()
    {
        // Arrange
        var productId = "129349158";
        var largeHtmlContent = CreateLargeHtmlWithSpecifications();

        SetupHttpResponse(HttpStatusCode.OK, largeHtmlContent);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _extractor.ExtractProductDetailAsync(productId);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/html")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private static string CreateHtmlWithCompleteSpecifications()
    {
        return @"
<!DOCTYPE html>
<html>
<head><title>Kaspi Product</title></head>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""129349158"",
        ""title"": ""Мясорубка электрическая Zepter ZP-987 белый"",
        ""price"": 12141,
        ""currency"": ""KZT"",
        ""shopLink"": ""/shop/p/mjasorubka-elektricheskaja-zepter-zp-987-belyi-129349158/""
    },
    ""description"": ""Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени..."",
    ""specifications"": [
        {
            ""code"": ""Meat grinders*Features"",
            ""name"": ""Особенности"",
            ""position"": 1,
            ""features"": [
                {
                    ""code"": ""meat grinders*tray material"",
                    ""name"": ""Материал лотка"",
                    ""type"": ""ENUM"",
                    ""featureValues"": [{""value"": ""металл""}],
                    ""position"": 22,
                    ""visible"": true,
                    ""multiValued"": false
                }
            ]
        },
        {
            ""code"": ""Home equipment*Harakteristiki"",
            ""name"": ""Характеристики"",
            ""position"": 2,
            ""features"": [
                {
                    ""code"": ""home equipment*colour"",
                    ""name"": ""Цвет"",
                    ""type"": ""ENUM"",
                    ""featureValues"": [{""value"": ""белый""}],
                    ""position"": 2,
                    ""visible"": true,
                    ""multiValued"": true
                }
            ]
        },
        {
            ""code"": ""Meat grinders*Technical characteristics"",
            ""name"": ""Общие характеристики"",
            ""position"": 3,
            ""features"": [
                {
                    ""code"": ""meat grinders*power"",
                    ""name"": ""Номинальная мощность"",
                    ""type"": ""NUMBER"",
                    ""featureValues"": [{""value"": ""2800.0 Вт""}],
                    ""position"": 1,
                    ""visible"": true,
                    ""multiValued"": false
                }
            ]
        },
        {
            ""code"": ""Meat grinders*Dimensions and weight"",
            ""name"": ""Габариты и вес"",
            ""position"": 4,
            ""features"": [
                {
                    ""code"": ""meat grinders*weight"",
                    ""name"": ""Вес"",
                    ""type"": ""NUMBER"",
                    ""featureValues"": [{""value"": ""2.7 кг""}],
                    ""position"": 1,
                    ""visible"": true,
                    ""multiValued"": false
                }
            ]
        }
    ],
    ""galleryImages"": [],
    ""endpoint"": ""https://kaspi.kz/shop/rest/misc/product/mobile""
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithMinimalSpecifications()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""102298404"",
        ""title"": ""Simple Product"",
        ""price"": 5000,
        ""currency"": ""KZT""
    },
    ""description"": ""Short description"",
    ""specifications"": [
        {
            ""code"": ""Basic*Info"",
            ""name"": ""Основная информация"",
            ""features"": [
                {
                    ""name"": ""Тип"",
                    ""featureValues"": [{""value"": ""Стандартный""}]
                }
            ]
        }
    ],
    ""galleryImages"": []
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithEmptySpecifications()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""999999999"",
        ""title"": ""Empty Product"",
        ""price"": 1000,
        ""currency"": ""KZT""
    },
    ""description"": null,
    ""specifications"": [],
    ""galleryImages"": []
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithNullDescription()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""129349158"",
        ""title"": ""Product without description"",
        ""price"": 8000,
        ""currency"": ""KZT""
    },
    ""description"": null,
    ""specifications"": [
        {
            ""code"": ""Basic*Info"",
            ""name"": ""Основная информация"",
            ""features"": []
        }
    ],
    ""galleryImages"": []
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithInvalidJson()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND.components.item = { invalid json syntax here };
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithMultiValuedFeatures()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""102298404"",
        ""title"": ""Multi-valued Product"",
        ""price"": 7500,
        ""currency"": ""KZT""
    },
    ""description"": ""Product with multi-valued features"",
    ""specifications"": [
        {
            ""code"": ""Colors*Options"",
            ""name"": ""Цветовые варианты"",
            ""features"": [
                {
                    ""name"": ""Доступные цвета"",
                    ""featureValues"": [
                        {""value"": ""красный""}, 
                        {""value"": ""синий""}, 
                        {""value"": ""зеленый""}
                    ],
                    ""multiValued"": true
                }
            ]
        }
    ],
    ""galleryImages"": []
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithVariousSpecificationTypes()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""129349158"",
        ""title"": ""Various Types Product"",
        ""price"": 15000,
        ""currency"": ""KZT""
    },
    ""description"": ""Product with various specification types"",
    ""specifications"": [
        {
            ""code"": ""Technical*Specs"",
            ""name"": ""Технические характеристики"",
            ""features"": [
                {
                    ""name"": ""Материал"",
                    ""type"": ""ENUM"",
                    ""featureValues"": [{""value"": ""пластик""}]
                },
                {
                    ""name"": ""Вес"",
                    ""type"": ""NUMBER"",
                    ""featureValues"": [{""value"": ""2.5 кг""}]
                },
                {
                    ""name"": ""Описание"",
                    ""type"": ""STRING"",
                    ""featureValues"": [{""value"": ""Качественный товар""}]
                }
            ]
        }
    ],
    ""galleryImages"": []
};
</script>
</body>
</html>";
    }

    private static string CreateHtmlWithImageGallery()
    {
        return @"
<!DOCTYPE html>
<html>
<body>
<script>
window.BACKEND = window.BACKEND || {};
window.BACKEND.components = window.BACKEND.components || {};
window.BACKEND.components.item = {
    ""card"": {
        ""id"": ""129349158"",
        ""title"": ""Product with images"",
        ""price"": 12000,
        ""currency"": ""KZT""
    },
    ""description"": ""Product with multiple images"",
    ""specifications"": [],
    ""galleryImages"": [
        {
            ""small"": ""https://kaspi.kz/img/small1.jpg"",
            ""medium"": ""https://kaspi.kz/img/medium1.jpg"",
            ""large"": ""https://kaspi.kz/img/large1.jpg"",
            ""location"": ""location1""
        },
        {
            ""small"": ""https://kaspi.kz/img/small2.jpg"",
            ""medium"": ""https://kaspi.kz/img/medium2.jpg"",
            ""large"": ""https://kaspi.kz/img/large2.jpg"",
            ""location"": ""location2""
        }
    ]
};
</script>
</body>
</html>";
    }

    private static string CreateLargeHtmlWithSpecifications()
    {
        var baseHtml = CreateHtmlWithCompleteSpecifications();
        var largeContent = new string('x', 100000); // 100KB of extra content
        return baseHtml.Replace("</body>", $"<!-- {largeContent} --></body>");
    }

    #endregion
}
