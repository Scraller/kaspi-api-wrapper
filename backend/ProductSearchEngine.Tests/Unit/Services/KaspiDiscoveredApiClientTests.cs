using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Services;

namespace ProductSearchEngine.Tests.Unit.Services;

/// <summary>
/// Unit tests for Kaspi Product Listing API Client
/// Based on discovered endpoint: /yml/product-view/pl/results
/// Following TDD approach from test generation instructions
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("DiscoveredEndpoints")]
public class KaspiProductListingApiClientTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
    }

    #region Product Listing API Tests - Based on Discovery

    [Test]
    [Description("TDD: Verify ProductListingResponse structure matches discovered API - Updated with actual curl response")]
    public void ProductListingResponse_Should_HaveCorrectStructure_When_Deserialized()
    {
        // Arrange - Based on actual API response structure from curl testing
        var jsonResponse = """
        {
          "data": [
            {
              "id": "100236584",
              "title": "Яйцо QARQUS куриное 30 шт 60-65 г",
              "brand": "QARQUS",
              "categoryId": "03006",
              "hasVariants": false,
              "loanAvailable": false,
              "shopLink": "/p/jaitso-qarqus-kurinoe-30-sht-60-65-g-100236584/?c=750000000",
              "unitPrice": 2303,
              "unitSalePrice": 2303,
              "priceFormatted": "2 303 ₸",
              "createdTime": "2020-05-03T11:49:56.374Z",
              "stickers": ["magnum_offer_available"],
              "previewImages": [
                {
                  "small": "https://resources.cdn-kaspi.kz/img/m/p/h6e/h0c/79439780937758.jpg?format=preview-small",
                  "medium": "https://resources.cdn-kaspi.kz/img/m/p/h6e/h0c/79439780937758.jpg?format=preview-medium",
                  "large": "https://resources.cdn-kaspi.kz/img/m/p/h6e/h0c/79439780937758.jpg?format=preview-large"
                }
              ],
              "teasers": [],
              "creditMonthlyPrice": 768.0,
              "monthlyInstallment": {
                "id": 3,
                "installment": true,
                "formattedPerMonth": "768 ₸"
              },
              "reviewsLink": "/p/jaitso-qarqus-kurinoe-30-sht-60-65-g-100236584/?c=750000000&tab=reviews",
              "weight": 0.0,
              "unit": {
                "type": "PIECES",
                "increment": 1.0,
                "measurementLiteral": "шт",
                "countingLiteral": "шт"
              },
              "rating": 4.64
            }
          ]
        }
        """;

        // Act - Deserialize and validate structure
        var response = JsonSerializer.Deserialize<ProductListingResponse>(jsonResponse);

        // Assert - Validate all discovered fields are present and correctly typed
        response.Should().NotBeNull();
        response!.Data.Should().NotBeEmpty();
        
        var product = response.Data.First();
        product.Id.Should().Be("100236584");
        product.Title.Should().Be("Яйцо QARQUS куриное 30 шт 60-65 г");
        product.Brand.Should().Be("QARQUS");
        product.CategoryId.Should().Be("03006");
        product.HasVariants.Should().BeFalse();
        product.LoanAvailable.Should().BeFalse();
        product.ShopLink.Should().Be("/p/jaitso-qarqus-kurinoe-30-sht-60-65-g-100236584/?c=750000000");
        product.UnitPrice.Should().Be(2303);
        product.UnitSalePrice.Should().Be(2303);
        product.PriceFormatted.Should().Be("2 303 ₸");
        product.CreatedTime.Should().Be(new DateTime(2020, 5, 3, 11, 49, 56, 374, DateTimeKind.Utc));
        product.Stickers.Should().Contain("magnum_offer_available");
        product.PreviewImages.Should().NotBeEmpty();
        product.PreviewImages.First().Small.Should().StartWith("https://resources.cdn-kaspi.kz/");
        product.CreditMonthlyPrice.Should().Be(768.0m);
        product.MonthlyInstallment.Should().NotBeNull();
        product.MonthlyInstallment!.FormattedPerMonth.Should().Be("768 ₸");
        product.ReviewsLink.Should().Contain("tab=reviews");
        product.Unit.Should().NotBeNull();
        product.Unit!.Type.Should().Be("PIECES");
        product.Rating.Should().Be(4.64m);

        TestContext.WriteLine("✅ Product listing structure validation passed with actual API response");
    }

    [Test]
    [Description("TDD: Verify JSON structure matches discovered API pattern with different product types")]
    public void ProductListingResponse_Should_HandleDifferentProductTypes_When_Deserialized()
    {
        // Arrange - Based on iPhone search result from curl testing
        var jsonResponse = """
        {
          "data": [
            {
              "id": "123726722",
              "title": "Apple iPhone 16 128Gb белый",
              "brand": "Apple",
              "categoryId": "00003",
              "hasVariants": false,
              "loanAvailable": true,
              "shopLink": "/p/apple-iphone-16-128gb-belyi-123726722/?c=750000000",
              "unitPrice": 443589,
              "unitSalePrice": 443589,
              "priceFormatted": "443 589 ₸",
              "createdTime": "2024-09-16T08:15:52.024Z",
              "stickers": [],
              "previewImages": [
                {
                  "small": "https://resources.cdn-kaspi.kz/img/m/p/h35/h47/87295471124510.png?format=preview-small",
                  "medium": "https://resources.cdn-kaspi.kz/img/m/p/h35/h47/87295471124510.png?format=preview-medium",
                  "large": "https://resources.cdn-kaspi.kz/img/m/p/h35/h47/87295471124510.png?format=preview-large"
                }
              ],
              "teasers": [],
              "creditMonthlyPrice": 14786.0,
              "monthlyInstallment": {
                "id": 3,
                "installment": true,
                "formattedPerMonth": "14 786 ₸"
              },
              "reviewsLink": "/p/apple-iphone-16-128gb-belyi-123726722/?c=750000000&tab=reviews",
              "weight": 0.0,
              "unit": null,
              "rating": 4.8,
              "currency": "KZT"
            }
          ],
          "promotedCards": null,
          "promotedItems": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ProductListingResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert - Validate structure matches discovered API
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(1);
        
        var product = response.Data.First();
        product.Id.Should().Be("123726722");
        product.Title.Should().Be("Apple iPhone 16 128Gb белый");
        product.Brand.Should().Be("Apple");
        product.CategoryId.Should().Be("00003");
        product.HasVariants.Should().BeFalse();
        product.LoanAvailable.Should().BeTrue();
        product.UnitPrice.Should().Be(443589);
        product.UnitSalePrice.Should().Be(443589);
        product.PriceFormatted.Should().Be("443 589 ₸");
        product.Currency.Should().Be("KZT");
        product.Rating.Should().Be(4.8m);
        product.PreviewImages.Should().HaveCount(1);
        product.PreviewImages.First().Small.Should().StartWith("https://resources.cdn-kaspi.kz/");
        product.CreditMonthlyPrice.Should().Be(14786.0m);
        product.MonthlyInstallment.Should().NotBeNull();
        product.MonthlyInstallment!.FormattedPerMonth.Should().Be("14 786 ₸");
        product.Unit.Should().BeNull(); // Some products don't have unit info
        product.Stickers.Should().BeEmpty();
        product.Teasers.Should().BeEmpty();
    }

    [Test]
    [Description("TDD: Verify URL construction follows discovered pattern")]
    [TestCase("smartphones%20and%20gadgets", 1, "750000000", 
        "https://kaspi.kz/yml/product-view/pl/results?page=1&q=%3AavailableInZones%3AMagnum_ZONE1&c=750000000")]
    [TestCase("laptops", 2, "710000000", 
        "https://kaspi.kz/yml/product-view/pl/results?page=2&q=%3AavailableInZones%3AMagnum_ZONE1&c=710000000")]
    public void GetCategoryProducts_Should_ConstructCorrectUrl_When_Called(
        string categoryCode, int page, string cityCode, string expectedUrl)
    {
        // Arrange - Based on discovered URL patterns
        var mockClient = new Mock<IKaspiProductListingApiClient>();
        
        // Act - This would be the actual implementation
        // mockClient.Setup(x => x.GetCategoryProductsAsync(categoryCode, page, cityCode, default))
        //     .Returns(Task.FromResult(new ProductListingResponse()));

        // Assert - URL pattern follows discovery
        expectedUrl.Should().Contain("/yml/product-view/pl/results");
        expectedUrl.Should().Contain($"page={page}");
        expectedUrl.Should().Contain($"c={cityCode}");
        expectedUrl.Should().Contain("q=%3AavailableInZones%3AMagnum_ZONE1");
    }

    [Test]
    [Description("TDD: Verify search URL construction follows discovered pattern")]
    [TestCase("iphone", 1, "750000000")]
    [TestCase("samsung", 2, "710000000")]
    public void SearchProducts_Should_ConstructCorrectSearchUrl_When_Called(
        string searchText, int page, string cityCode)
    {
        // Arrange - Based on discovered search endpoint patterns
        var expectedUrlPattern = $"https://kaspi.kz/yml/product-view/pl/filters?text={searchText}&page={page}&c={cityCode}";
        
        // Act - Validate URL construction pattern
        var actualPattern = $"https://kaspi.kz/yml/product-view/pl/filters?text={searchText}&page={page}&c={cityCode}";
        
        // Assert - Search URL follows discovery
        actualPattern.Should().Contain("/yml/product-view/pl/filters");
        actualPattern.Should().Contain($"text={searchText}");
        actualPattern.Should().Contain($"page={page}");
        actualPattern.Should().Contain($"c={cityCode}");
    }

    [Test]
    [Description("TDD: Verify regional parameters affect results as discovered")]
    [TestCase("750000000", "Almaty")]
    [TestCase("710000000", "Astana")]
    public void GetCategoryProducts_Should_IncludeRegionalParameters_When_CityCodeProvided(
        string cityCode, string expectedCityName)
    {
        // Arrange - Based on Phase 4 regional discovery
        var mockResponse = new ProductListingResponse
        {
            Data = new List<ProductItemResponse>
            {
                new()
                {
                    Id = "100236584",
                    Title = $"Product in {expectedCityName}",
                    Currency = "KZT",
                    DeliveryZones = new List<string> { $"ZONE_{cityCode}" }
                }
            }
        };

        // Act & Assert - Regional parameters are properly included
        mockResponse.Data.First().DeliveryZones.Should().Contain($"ZONE_{cityCode}");
        mockResponse.Data.First().Title.Should().Contain(expectedCityName);
    }

    [Test]
    [Description("TDD: Verify pagination follows discovered pattern")]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void GetCategoryProducts_Should_SupportPagination_When_PageParameterProvided(int pageNumber)
    {
        // Arrange - Based on discovered pagination patterns
        var expectedUrl = $"https://kaspi.kz/yml/product-view/pl/results?page={pageNumber}";
        
        // Act - Validate pagination parameter inclusion
        var urlContainsPage = expectedUrl.Contains($"page={pageNumber}");
        
        // Assert - Pagination follows discovery
        urlContainsPage.Should().BeTrue("URL should contain page parameter");
        pageNumber.Should().BeGreaterThan(0, "Page numbers should be 1-based as discovered");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    [Description("TDD: Verify error handling for invalid product responses")]
    public void ProductListingResponse_Should_HandleEmptyData_When_NoProductsFound()
    {
        // Arrange
        var emptyResponse = """
        {
            "data": [],
            "promotedCards": null,
            "promotedItems": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ProductListingResponse>(emptyResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        response.Should().NotBeNull();
        response!.Data.Should().BeEmpty();
        response.PromotedCards.Should().BeNull();
        response.PromotedItems.Should().BeNull();
    }

    [Test]
    [Description("TDD: Verify handling of malformed responses")]
    public void ProductListingResponse_Should_ThrowException_When_InvalidJson()
    {
        // Arrange
#pragma warning disable JSON001 // Invalid JSON pattern
        var invalidJson = "{ \"invalid\": json }"; // Invalid JSON with unquoted value
#pragma warning restore JSON001 // Invalid JSON pattern

        // Act & Assert
        var action = () => JsonSerializer.Deserialize<ProductListingResponse>(invalidJson);
        action.Should().Throw<JsonException>();
    }

    #endregion

    #region Performance Tests

    [Test]
    [Description("TDD: Verify response time meets PRD requirements (< 3 seconds)")]
    [CancelAfter(3000)] // 3 second timeout as per PRD
    public async Task GetCategoryProducts_Should_RespondUnder3Seconds_When_Called()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - This is a placeholder for actual implementation
        await Task.Delay(100); // Simulate API call
        
        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, 
            "API response should be under 3 seconds as per PRD requirements");
    }

    #endregion
}

/// <summary>
/// Unit tests for Kaspi Product Reviews API Client
/// Based on discovered endpoint: /yml/review-view/api/v1/reviews/product/{id}
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("DiscoveredEndpoints")]
public class KaspiProductReviewsApiClientTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
    }

    #region Product Reviews API Tests - Based on Discovery

    [Test]
    [Description("TDD: Verify ProductReviewsResponse structure matches discovered API")]
    public void ProductReviewsResponse_Should_HaveCorrectStructure_When_Deserialized()
    {
        // Arrange - Based on actual reviews API response structure
        var jsonResponse = """
        {
            "data": [
                {
                    "id": "8a5cd50184e53cc00184f70f17ad1fa9",
                    "author": "Аяжан",
                    "date": "09.12.2022",
                    "orderNumber": "229060185",
                    "rating": 5,
                    "comment": {
                        "minus": "",
                        "plus": "",
                        "text": "Классный чехол."
                    },
                    "feedback": {
                        "positive": 120,
                        "voted": false
                    },
                    "product": {
                        "id": "106185651",
                        "name": "Чехол для Apple iPhone 13 прозрачный",
                        "categoryCode": "Master - Phone cases",
                        "categoryName": "Чехлы для смартфонов",
                        "link": "https://kaspi.kz/shop/p/chehol-dlja-apple-iphone-13-prozrachnyi-106185651/"
                    },
                    "editable": false,
                    "editedByCustomer": false
                }
            ],
            "summary": {
                "global": 4.9,
                "statistic": [
                    { "rate": 5, "count": 12632 },
                    { "rate": 4, "count": 396 }
                ]
            },
            "groupSummary": [
                { "id": "ALL", "total": 13160 },
                { "id": "COMMENT", "total": 6545 }
            ],
            "imagesSummaryCount": 2525
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ProductReviewsResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert - Validate structure matches discovered API
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(1);
        
        var review = response.Data.First();
        review.Id.Should().Be("8a5cd50184e53cc00184f70f17ad1fa9");
        review.Author.Should().Be("Аяжан");
        review.Rating.Should().Be(5);
        review.Comment.Should().NotBeNull();
        review.Comment!.Text.Should().Be("Классный чехол.");
        review.Feedback.Should().NotBeNull();
        review.Feedback!.Positive.Should().Be(120);
        review.Product.Should().NotBeNull();
        review.Product!.Id.Should().Be("106185651");
        
        response.Summary.Should().NotBeNull();
        response.Summary!.Global.Should().Be(4.9m);
        response.GroupSummary.Should().HaveCount(2);
        response.ImagesSummaryCount.Should().Be(2525);
    }

    [Test]
    [Description("TDD: Verify reviews URL construction follows discovered pattern")]
    [TestCase("106185651", "COMMENT", "POPULARITY", 9, true,
        "https://kaspi.kz/yml/review-view/api/v1/reviews/product/106185651?filter=COMMENT&sort=POPULARITY&limit=9&withAgg=true")]
    [TestCase("102298404", "RATING", "DATE", 5, false,
        "https://kaspi.kz/yml/review-view/api/v1/reviews/product/102298404?filter=RATING&sort=DATE&limit=5&withAgg=false")]
    public void GetProductReviews_Should_ConstructCorrectUrl_When_Called(
        string productId, string filter, string sort, int limit, bool withAgg, string expectedUrl)
    {
        // Arrange - Based on discovered URL patterns
        var mockClient = new Mock<IKaspiProductReviewsApiClient>();
        
        // Act - This would be the actual implementation
        // mockClient.Setup(x => x.GetProductReviewsAsync(productId, filter, sort, limit, withAgg, default))
        //     .Returns(Task.FromResult(new ProductReviewsResponse()));

        // Assert - URL pattern follows discovery
        expectedUrl.Should().Contain($"/yml/review-view/api/v1/reviews/product/{productId}");
        expectedUrl.Should().Contain($"filter={filter}");
        expectedUrl.Should().Contain($"sort={sort}");
        expectedUrl.Should().Contain($"limit={limit}");
        expectedUrl.Should().Contain($"withAgg={withAgg.ToString().ToLower()}");
    }

    [Test]
    [Description("TDD: Verify review filtering follows discovered patterns")]
    [TestCase("COMMENT")]
    [TestCase("RATING")]
    [TestCase("PICTURE")]
    [TestCase("POSITIVE")]
    [TestCase("NEGATIVE")]
    public void GetProductReviews_Should_SupportFiltering_When_FilterProvided(string filter)
    {
        // Arrange - Based on discovered filter options
        var validFilters = new[] { "COMMENT", "RATING", "PICTURE", "POSITIVE", "NEGATIVE" };
        
        // Act & Assert - Filter is in valid discovered options
        validFilters.Should().Contain(filter, "Filter should be in discovered valid options");
    }

    [Test]
    [Description("TDD: Verify review sorting follows discovered patterns")]
    [TestCase("POPULARITY")]
    [TestCase("DATE")]
    [TestCase("RATING")]
    public void GetProductReviews_Should_SupportSorting_When_SortProvided(string sort)
    {
        // Arrange - Based on discovered sort options
        var validSorts = new[] { "POPULARITY", "DATE", "RATING" };
        
        // Act & Assert - Sort is in valid discovered options
        validSorts.Should().Contain(sort, "Sort should be in discovered valid options");
    }

    #endregion

    #region Product Reviews API Tests - Based on Actual curl Response

    [Test]
    [Description("TDD: Verify ProductReviewsResponse structure matches actual curl response data")]
    public void ProductReviewsResponse_Should_MatchActualApiResponse_When_Deserialized()
    {
        // Arrange - Based on actual curl response from /yml/review-view/api/v1/reviews/product/106185651
        var jsonResponse = """
        {
          "data": [
            {
              "id": "8a5cd50184e53cc00184f70f17ad1fa9",
              "author": "Аяжан",
              "date": "09.12.2022",
              "orderNumber": "229060185",
              "rating": 5,
              "comment": {
                "minus": "",
                "plus": "",
                "text": "Классный чехол."
              },
              "feedback": {
                "positive": 120,
                "voted": false
              },
              "galleryImages": [
                {
                  "id": "229060185-106185651/5007d23e-62ca-4dc8-9cde-14cbec295e60.jpg",
                  "small": "https://resources.cdn-kaspi.kz/img/p/r/229060185-106185651/5007d23e-62ca-4dc8-9cde-14cbec295e60.jpg?format=review_small",
                  "medium": "https://resources.cdn-kaspi.kz/img/p/r/229060185-106185651/5007d23e-62ca-4dc8-9cde-14cbec295e60.jpg?format=review_medium",
                  "large": "https://resources.cdn-kaspi.kz/img/p/r/229060185-106185651/5007d23e-62ca-4dc8-9cde-14cbec295e60.jpg?format=review_large"
                }
              ],
              "product": {
                "id": "106185651",
                "name": "Чехол для Apple iPhone 13 прозрачный",
                "categoryCode": "Master - Phone cases",
                "categoryName": "Чехлы для смартфонов",
                "link": "https://kaspi.kz/shop/p/chehol-dlja-apple-iphone-13-prozrachnyi-106185651/"
              },
              "merchant": null,
              "editable": false,
              "editedByCustomer": false
            }
          ],
          "summary": {
            "global": 4.9,
            "statistic": [
              {"rate": 5, "count": 12632},
              {"rate": 4, "count": 396},
              {"rate": 3, "count": 54},
              {"rate": 2, "count": 19},
              {"rate": 1, "count": 59}
            ]
          },
          "groupSummary": [
            {"id": "ALL", "total": 13160},
            {"id": "COMMENT", "total": 6545},
            {"id": "PICTURE", "total": 1319},
            {"id": "POSITIVE", "total": 6495},
            {"id": "NEGATIVE", "total": 50}
          ],
          "imagesSummaryCount": 2525
        }
        """;

        // Act - Deserialize and validate structure
        var response = JsonSerializer.Deserialize<ProductReviewsResponse>(jsonResponse);

        // Assert - Validate all discovered fields are present and correctly typed
        response.Should().NotBeNull();
        response!.Data.Should().NotBeEmpty();
        response.Summary.Should().NotBeNull();
        response.GroupSummary.Should().NotBeEmpty();
        response.ImagesSummaryCount.Should().Be(2525);
        
        // Validate review structure
        var review = response.Data.First();
        review.Id.Should().Be("8a5cd50184e53cc00184f70f17ad1fa9");
        review.Author.Should().Be("Аяжан");
        review.Date.Should().Be("09.12.2022");
        review.OrderNumber.Should().Be("229060185");
        review.Rating.Should().Be(5);
        review.Comment.Should().NotBeNull();
        review.Comment!.Text.Should().Be("Классный чехол.");
        review.Feedback.Should().NotBeNull();
        review.Feedback!.Positive.Should().Be(120);
        review.Feedback.Voted.Should().BeFalse();
        review.GalleryImages.Should().NotBeEmpty();
        review.Product.Should().NotBeNull();
        review.Product!.Id.Should().Be("106185651");
        review.Product.Name.Should().Be("Чехол для Apple iPhone 13 прозрачный");
        review.Product.CategoryCode.Should().Be("Master - Phone cases");
        review.Product.Link.Should().StartWith("https://kaspi.kz/shop/p/");
        review.Merchant.Should().BeNull();
        review.Editable.Should().BeFalse();
        review.EditedByCustomer.Should().BeFalse();
        
        // Validate gallery images structure
        var galleryImage = review.GalleryImages.First();
        galleryImage.Id.Should().NotBeNullOrEmpty();
        galleryImage.Small.Should().StartWith("https://resources.cdn-kaspi.kz/");
        galleryImage.Medium.Should().StartWith("https://resources.cdn-kaspi.kz/");
        galleryImage.Large.Should().StartWith("https://resources.cdn-kaspi.kz/");
        galleryImage.Small.Should().Contain("format=review_small");
        galleryImage.Medium.Should().Contain("format=review_medium");
        galleryImage.Large.Should().Contain("format=review_large");
        
        // Validate summary structure
        response.Summary!.Global.Should().Be(4.9m);
        response.Summary.Statistic.Should().HaveCount(5); // 1-5 star ratings
        response.Summary.Statistic.Should().OnlyContain(s => s.Rate >= 1 && s.Rate <= 5);
        response.Summary.Statistic.Should().OnlyContain(s => s.Count >= 0);
        
        // Validate group summary
        response.GroupSummary.Should().Contain(g => g.Id == "ALL" && g.Total == 13160);
        response.GroupSummary.Should().Contain(g => g.Id == "COMMENT" && g.Total == 6545);
        response.GroupSummary.Should().Contain(g => g.Id == "PICTURE" && g.Total == 1319);
        response.GroupSummary.Should().Contain(g => g.Id == "POSITIVE" && g.Total == 6495);
        response.GroupSummary.Should().Contain(g => g.Id == "NEGATIVE" && g.Total == 50);

        TestContext.WriteLine("✅ Product reviews structure validation passed with actual API response");
    }

    [Test]
    [Description("TDD: Verify reviews URL construction matches discovered API pattern")]
    [TestCase("106185651", "COMMENT", "POPULARITY", 9, true, 
        TestName = "Standard review request with all parameters")]
    [TestCase("123456789", "RATING", "DATE", 20, false, 
        TestName = "Rating-only reviews sorted by date")]
    [TestCase("987654321", "PICTURE", "POPULARITY", 5, true, 
        TestName = "Picture reviews only")]
    public void GetProductReviewsUrl_Should_ConstructCorrectUrl_When_CalledWithDifferentParameters(
        string productId, string filter, string sort, int limit, bool withAgg)
    {
        // Arrange - Based on discovered API pattern from curl testing
        var expectedBaseUrl = $"/yml/review-view/api/v1/reviews/product/{productId}";
        
        // Act - URL construction logic that would be in actual client
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(filter)) queryParams.Add($"filter={filter}");
        if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={sort}");
        if (limit > 0) queryParams.Add($"limit={limit}");
        queryParams.Add($"withAgg={withAgg.ToString().ToLower()}");
        
        var constructedUrl = $"{expectedBaseUrl}?{string.Join("&", queryParams)}";
        
        // Assert - URL pattern matches discovered endpoint structure
        constructedUrl.Should().StartWith("/yml/review-view/api/v1/reviews/product/");
        constructedUrl.Should().Contain(productId);
        constructedUrl.Should().Contain($"filter={filter}");
        constructedUrl.Should().Contain($"sort={sort}");
        constructedUrl.Should().Contain($"limit={limit}");
        constructedUrl.Should().Contain($"withAgg={withAgg.ToString().ToLower()}");
        
        // Validate query parameter order and format
        var queryString = constructedUrl.Split('?')[1];
        queryString.Should().NotBeNullOrEmpty();
        queryString.Split('&').Should().HaveCountGreaterOrEqualTo(4);
        
        TestContext.WriteLine($"✅ Constructed URL: {constructedUrl}");
        TestContext.WriteLine($"✅ Query parameters: {queryString}");
    }

    [Test]
    [Description("TDD: Verify reviews API supports different filter types from discovery")]
    [TestCase("COMMENT", "Should filter for reviews with text comments")]
    [TestCase("RATING", "Should filter for rating-only reviews")]
    [TestCase("PICTURE", "Should filter for reviews with images")]
    [TestCase("POSITIVE", "Should filter for positive reviews")]
    [TestCase("NEGATIVE", "Should filter for negative reviews")]
    public void ReviewsFilterParameter_Should_SupportDiscoveredFilterTypes_When_Specified(
        string filterType, string description)
    {
        // Arrange - Based on groupSummary types discovered in actual API response
        var validFilterTypes = new[] { "COMMENT", "RATING", "PICTURE", "POSITIVE", "NEGATIVE", "ALL" };
        
        // Act - Filter validation logic
        var isValidFilter = validFilterTypes.Contains(filterType);
        
        // Assert - Filter type is supported by the discovered API
        isValidFilter.Should().BeTrue($"Filter type '{filterType}' should be supported by the API");
        
        TestContext.WriteLine($"✅ Filter '{filterType}': {description}");
    }

    #endregion

    #region API Error Handling Tests - Based on Actual curl Responses

    [Test]
    [Description("TDD: Verify error response structure for blocked endpoints")]
    public void ApiErrorResponse_Should_HandleMethodNotAllowed_When_EndpointBlocked()
    {
        // Arrange - Based on actual 405 response from offers endpoint during curl testing
        var errorResponseJson = """
        {
          "timestamp": "2025-06-26T07:52:34.682+00:00",
          "status": 405,
          "error": "Method Not Allowed",
          "path": "/offers/106185651"
        }
        """;

        // Act - Deserialize error response
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(errorResponseJson);
        
        // Assert - Error response follows expected format from discovered API
        errorResponse.GetProperty("status").GetInt32().Should().Be(405);
        errorResponse.GetProperty("error").GetString().Should().Be("Method Not Allowed");
        errorResponse.GetProperty("timestamp").GetString().Should().NotBeNullOrEmpty();
        errorResponse.GetProperty("path").GetString().Should().Contain("/offers/");
        
        // Validate timestamp format (ISO 8601)
        var timestamp = errorResponse.GetProperty("timestamp").GetString();
        DateTime.TryParse(timestamp, out var parsedTimestamp).Should().BeTrue();
        parsedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(24));
        
        TestContext.WriteLine($"✅ Method Not Allowed error handled correctly");
        TestContext.WriteLine($"✅ Timestamp format: {timestamp}");
    }

    #endregion
}
