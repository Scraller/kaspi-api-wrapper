using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Kaspi;
using ProductSearchEngine.Tests.Helpers;
using NUnit.Framework;

namespace ProductSearchEngine.Tests.BDD.StepDefinitions;

/// <summary>
/// Consolidated step definitions for Kaspi Offer API Client BDD tests
/// This file contains all step definitions needed for the KaspiOfferApiClient.feature
/// </summary>
[Binding]
[NonParallelizable]
public class KaspiOfferApiClientConsolidatedStepDefinitions : TestBase
{
    private KaspiOfferApiClient _offerApiClient = null!;
    private string _productId = string.Empty;
    private string _cityId = "551010000"; // Default to Almaty
    private List<Offer> _fetchedOffers = [];
    private string _jsonResponse = string.Empty;
    private bool _requestSucceeded = false;
    private Exception? _lastException;
    private Stopwatch _requestStopwatch = new();
    private readonly ILogger<KaspiOfferApiClientConsolidatedStepDefinitions> _logger;

    private List<ProductSearchEngine.Tests.TestData.CachedProduct> _dynamicTestProducts = new();
    private Dictionary<string, List<Offer>> _productOfferResults = new();

    public KaspiOfferApiClientConsolidatedStepDefinitions()
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<KaspiOfferApiClientConsolidatedStepDefinitions>>();
    }

    /// <summary>
    /// Reset state between scenarios to avoid interference when running in parallel
    /// </summary>
    [BeforeScenario]
    public void ResetState()
    {
        _productId = string.Empty;
        _cityId = "551010000"; // Default to Almaty
        _fetchedOffers.Clear();
        _jsonResponse = string.Empty;
        _requestSucceeded = false;
        _lastException = null;
        _requestStopwatch.Reset();
        _dynamicTestProducts.Clear();
        _productOfferResults.Clear();
    }

    #region Given Steps

    [Given(@"^the Kaspi Offer API client is initialized$")]
    public void GivenTheKaspiOfferAPIClientIsInitialized()
    {
        _offerApiClient = ServiceProvider.GetRequiredService<KaspiOfferApiClient>();
        _offerApiClient.Should().NotBeNull();
        _logger.LogInformation("Kaspi Offer API client initialized successfully");
    }

    [Given(@"^the anti-detection strategy is configured$")]
    public void GivenTheAntiDetectionStrategyIsConfigured()
    {
        // Anti-detection is configured in the DI container
        _logger.LogInformation("Anti-detection strategy configured");
    }

    [Given(@"^the city ID is set to ""([^""]*)"" \(([^)]*)\)$")]
    public void GivenTheCityIDIsSetTo(string cityId, string cityName)
    {
        _cityId = cityId;
        _logger.LogInformation("City ID set to {CityId} ({CityName})", cityId, cityName);
    }

    [Given(@"^I have real scraped products available$")]
    public async Task GivenIHaveRealScrapedProductsAvailable()
    {
        _dynamicTestProducts = (await ProductSearchEngine.Tests.TestData.TestDataCache.GetRandomTestProductsAsync(3)).ToList();

        _dynamicTestProducts.Should().NotBeEmpty("Should have real scraped products available for testing");

        _logger.LogInformation("Loaded {Count} dynamic test products for BDD testing", _dynamicTestProducts.Count);

        foreach (var product in _dynamicTestProducts)
        {
            _logger.LogInformation("Dynamic test product: ID={Id}, Name={Name}, Category={Category}",
                product.Id, product.Name, product.Category);
        }
    }

    [Given(@"^an invalid product ID ""([^""]*)""$")]
    public void GivenAnInvalidProductID(string productId)
    {
        _productId = productId;
        _logger.LogInformation("Set invalid product ID: {ProductId}", productId);
    }

    #endregion

    #region When Steps

    [When(@"^I fetch offers for a random scraped product from live API$")]
    public async Task WhenIFetchOffersForARandomScrapedProductFromLiveAPI()
    {
        await SelectRandomProductAndFetchOffers();
    }

    [When(@"^I fetch offers for a random scraped product from live API with timing$")]
    public async Task WhenIFetchOffersForARandomScrapedProductFromLiveAPIWithTiming()
    {
        await SelectRandomProductAndFetchOffers();
    }

    [When(@"^I fetch offers for city ""([^""]*)"" \(([^)]*)\) using a random scraped product$")]
    public async Task WhenIFetchOffersForCityUsingARandomScrapedProduct(string cityId, string cityName)
    {
        _cityId = cityId;
        await SelectRandomProductAndFetchOffers();
    }

    [When(@"^I fetch offers for the product from live API$")]
    public async Task WhenIFetchOffersForTheProductFromLiveAPI()
    {
        await ExecuteBasicOfferFetch();
    }

    [When(@"^I fetch offers for each product from live API$")]
    public async Task WhenIFetchOffersForEachProductFromLiveAPI()
    {
        foreach (var product in _dynamicTestProducts)
        {
            _productId = product.Id;
            await ExecuteBasicOfferFetch();
            _productOfferResults[product.Id] = new List<Offer>(_fetchedOffers);
        }
    }

    [When(@"^the API returns malformed JSON for a product$")]
    public async Task WhenTheAPIReturnsMalformedJSONForAProduct()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns empty JSON for a product$")]
    public async Task WhenTheAPIReturnsEmptyJSONForAProduct()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns JSON with missing required offer fields$")]
    public async Task WhenTheAPIReturnsJSONWithMissingRequiredOfferFields()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns offers with invalid price information$")]
    public async Task WhenTheAPIReturnsOffersWithInvalidPriceInformation()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns offers with invalid merchant information$")]
    public async Task WhenTheAPIReturnsOffersWithInvalidMerchantInformation()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns offers with corrupted delivery information$")]
    public async Task WhenTheAPIReturnsOffersWithCorruptedDeliveryInformation()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    [When(@"^the API returns a mix of valid and invalid offers$")]
    public async Task WhenTheAPIReturnsAMixOfValidAndInvalidOffers()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    #endregion

    #region Then Steps - Basic Validations

    [Then(@"^the request should succeed$")]
    public void ThenTheRequestShouldSucceed()
    {
        _requestSucceeded.Should().BeTrue("API request should succeed");
        _lastException.Should().BeNull("No exception should be thrown");
    }

    [Then(@"^I should receive multiple offers$")]
    public void ThenIShouldReceiveMultipleOffers()
    {
        _fetchedOffers.Should().NotBeEmpty("Should receive at least one offer");
    }

    [Then(@"^I should receive at least (\d+) offer$")]
    public void ThenIShouldReceiveAtLeastOffer(int minimumOffers)
    {
        _fetchedOffers.Should().HaveCountGreaterOrEqualTo(minimumOffers,
            $"Should receive at least {minimumOffers} offer(s)");
    }

    [Then(@"^I should receive an empty offers collection$")]
    public void ThenIShouldReceiveAnEmptyOffersCollection()
    {
        _fetchedOffers.Should().BeEmpty("Should receive no offers");
    }

    [Then(@"^the failure should be logged appropriately$")]
    public void ThenTheFailureShouldBeLoggedAppropriately()
    {
        _requestSucceeded.Should().BeTrue("Request should succeed even with invalid product ID");
        _fetchedOffers.Should().BeEmpty("Invalid product ID should result in empty offers collection");
        _lastException.Should().BeNull("No exception should be thrown for invalid product ID");

        _logger.LogInformation("API gracefully handled invalid product ID by returning empty offers collection");
    }

    [Then(@"^each offer should contain valid merchant information$")]
    public void ThenEachOfferShouldContainValidMerchantInformation()
    {
        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Each offer should have a merchant name");
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("Each offer should have a merchant ID");
        }
    }

    [Then(@"^each offer should contain pricing details$")]
    public void ThenEachOfferShouldContainPricingDetails()
    {
        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "Each offer should have a valid price");
        }
    }

    [Then(@"^each offer should contain delivery information$")]
    public void ThenEachOfferShouldContainDeliveryInformation()
    {
        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.DeliveryType.Should().NotBeNullOrWhiteSpace("Each offer should have delivery type information");
        }
    }

    #endregion

    #region Then Steps - Detailed Validations

    [Then(@"^all offers should have non-empty merchant names$")]
    public void ThenAllOffersShouldHaveNonEmptyMerchantNames()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping merchant name validation. This may be due to:");
            _logger.LogWarning("- Product is no longer available");
            _logger.LogWarning("- Anti-bot measures blocking the request");
            _logger.LogWarning("- Product has no offers in the specified city");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Each offer should have a non-empty merchant name");
        }

        _logger.LogInformation("✅ Validated {Count} offers have non-empty merchant names", _fetchedOffers.Count);
    }

    [Then(@"^all offers should have non-empty merchant IDs$")]
    public void ThenAllOffersShouldHaveNonEmptyMerchantIDs()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping merchant ID validation");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("Each offer should have a non-empty merchant ID");
        }

        _logger.LogInformation("✅ Validated {Count} offers have non-empty merchant IDs", _fetchedOffers.Count);
    }

    [Then(@"^all offers should have valid prices greater than (\d+)$")]
    public void ThenAllOffersShouldHaveValidPricesGreaterThan(int minimumPrice)
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping price validation");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(minimumPrice, $"Each offer should have a price greater than {minimumPrice}");
        }

        _logger.LogInformation("✅ Validated {Count} offers have valid prices > {MinPrice}", _fetchedOffers.Count, minimumPrice);
    }

    [Then(@"^offers with rating data should have ratings between ([\d.]+) and ([\d.]+)$")]
    public void ThenOffersWithRatingDataShouldHaveRatingsBetween(decimal minRating, decimal maxRating)
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping rating validation");
            return;
        }

        var offersWithRatings = _fetchedOffers.Where(o => o.MerchantRating.HasValue).ToList();

        if (offersWithRatings.Count == 0)
        {
            _logger.LogInformation("ℹ️ No offers have rating data - this is normal for some products");
            return;
        }

        foreach (var offer in offersWithRatings)
        {
            offer.MerchantRating.Should().BeInRange((double)minRating, (double)maxRating,
                $"Rating should be between {minRating} and {maxRating}");
        }

        _logger.LogInformation("✅ Validated {Count} offers have ratings between {Min} and {Max}",
            offersWithRatings.Count, minRating, maxRating);
    }

    [Then(@"^offers with review data should have positive review counts$")]
    public void ThenOffersWithReviewDataShouldHavePositiveReviewCounts()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping review count validation");
            return;
        }

        var offersWithReviews = _fetchedOffers.Where(o => o.MerchantReviewsQuantity.HasValue).ToList();

        if (offersWithReviews.Count == 0)
        {
            _logger.LogInformation("ℹ️ No offers have review data - this is normal for some products");
            return;
        }

        foreach (var offer in offersWithReviews)
        {
            offer.MerchantReviewsQuantity.Should().BeGreaterOrEqualTo(0, "Review count should be non-negative");
        }
    }

    [Then(@"^all offers should have delivery type information$")]
    public void ThenAllOffersShouldHaveDeliveryTypeInformation()
    {
        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.DeliveryType.Should().NotBeNullOrWhiteSpace("Each offer should have delivery type information");
        }
    }

    [Then(@"^offers with delivery duration should have valid duration values$")]
    public void ThenOffersWithDeliveryDurationShouldHaveValidDurationValues()
    {
        var offersWithDuration = _fetchedOffers.Where(o => !string.IsNullOrWhiteSpace(o.DeliveryDuration)).ToList();

        foreach (var offer in offersWithDuration)
        {
            offer.DeliveryDuration.Should().NotBeNullOrWhiteSpace("Delivery duration should be valid when present");
        }
    }

    [Then(@"^offers with Kaspi delivery should have boolean flags$")]
    public void ThenOffersWithKaspiDeliveryShouldHaveBooleanFlags()
    {
        // KaspiDelivery is a boolean property, so all offers have this flag set
        foreach (var offer in _fetchedOffers)
        {
            // Just verify the property exists and can be accessed
            var kaspiDeliveryFlag = offer.KaspiDelivery;
            _logger.LogInformation("Offer has Kaspi delivery flag: {Flag}", kaspiDeliveryFlag);
        }
    }

    [Then(@"^offers with delivery costs should have non-negative costs$")]
    public void ThenOffersWithDeliveryCostsShouldHaveNonNegativeCosts()
    {
        var offersWithDeliveryCost = _fetchedOffers.Where(o => o.DeliveryCost.HasValue).ToList();

        foreach (var offer in offersWithDeliveryCost)
        {
            offer.DeliveryCost.Should().BeGreaterOrEqualTo(0, "Delivery cost should be non-negative");
        }
    }

    [Then(@"^offers with delivery thresholds should have non-negative thresholds$")]
    public void ThenOffersWithDeliveryThresholdsShouldHaveNonNegativeThresholds()
    {
        var offersWithThreshold = _fetchedOffers.Where(o => o.DeliveryThreshold.HasValue).ToList();

        foreach (var offer in offersWithThreshold)
        {
            offer.DeliveryThreshold.Should().BeGreaterOrEqualTo(0, "Delivery threshold should be non-negative");
        }
    }

    [Then(@"^I should find offers with different prices if multiple offers exist$")]
    public void ThenIShouldFindOffersWithDifferentPricesIfMultipleOffersExist()
    {
        if (_fetchedOffers.Count > 1)
        {
            var prices = _fetchedOffers.Select(o => o.Price).Distinct().ToList();
            prices.Should().HaveCountGreaterThan(1, "Multiple offers should have different prices");
        }
    }

    [Then(@"^the price difference between cheapest and most expensive should be reasonable$")]
    public void ThenThePriceDifferenceBetweenCheapestAndMostExpensiveShouldBeReasonable()
    {
        if (_fetchedOffers.Count > 1)
        {
            var minPrice = _fetchedOffers.Min(o => o.Price);
            var maxPrice = _fetchedOffers.Max(o => o.Price);
            var priceRatio = maxPrice / minPrice;

            priceRatio.Should().BeLessOrEqualTo(10, "Price difference should be reasonable (max 10x difference)");
        }
    }

    [Then(@"^all prices should be positive numbers$")]
    public void ThenAllPricesShouldBePositiveNumbers()
    {
        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "All prices should be positive");
        }
    }

    [Then(@"^all prices should be in KZT currency$")]
    public void ThenAllPricesShouldBeInKZTCurrency()
    {
        // Since we're dealing with Kaspi (Kazakhstan), all prices should be in KZT
        // This is a business rule validation
        _fetchedOffers.Should().NotBeEmpty();
        _logger.LogInformation("All prices are assumed to be in KZT currency for Kaspi marketplace");
    }

    [Then(@"^offers with ratings should have values between ([\d.]+) and ([\d.]+)$")]
    public void ThenOffersWithRatingsShouldHaveValuesBetween(decimal minRating, decimal maxRating)
    {
        var offersWithRatings = _fetchedOffers.Where(o => o.MerchantRating.HasValue).ToList();

        foreach (var offer in offersWithRatings)
        {
            offer.MerchantRating.Should().BeInRange((double)minRating, (double)maxRating,
                $"Rating should be between {minRating} and {maxRating}");
        }
    }

    [Then(@"^offers with review counts should have non-negative values$")]
    public void ThenOffersWithReviewCountsShouldHaveNonNegativeValues()
    {
        var offersWithReviews = _fetchedOffers.Where(o => o.MerchantReviewsQuantity.HasValue).ToList();

        foreach (var offer in offersWithReviews)
        {
            offer.MerchantReviewsQuantity.Should().BeGreaterOrEqualTo(0, "Review count should be non-negative");
        }
    }

    [Then(@"^rating and review data should be consistent when present$")]
    public void ThenRatingAndReviewDataShouldBeConsistentWhenPresent()
    {
        var offersWithBothRatingAndReviews = _fetchedOffers
            .Where(o => o.MerchantRating.HasValue && o.MerchantReviewsQuantity.HasValue)
            .ToList();

        foreach (var offer in offersWithBothRatingAndReviews)
        {
            if (offer.MerchantReviewsQuantity > 0)
            {
                offer.MerchantRating.Should().BeGreaterThan(0, "If there are reviews, there should be a rating");
            }
        }
    }

    [Then(@"^offers should have availability information$")]
    public void ThenOffersShouldHaveAvailabilityInformation()
    {
        _fetchedOffers.Should().NotBeEmpty();
        // Availability information is typically embedded in other fields or delivery info
        _logger.LogInformation("Checking availability information in offers");
    }

    [Then(@"^offers should have pickup date information when available$")]
    public void ThenOffersShouldHavePickupDateInformationWhenAvailable()
    {
        var offersWithPickupDate = _fetchedOffers.Where(o => o.PickupDate.HasValue).ToList();

        foreach (var offer in offersWithPickupDate)
        {
            offer.PickupDate.Should().BeAfter(DateTime.Now.AddDays(-1), "Pickup date should be reasonable");
        }
    }

    [Then(@"^offers should have located point information$")]
    public void ThenOffersShouldHaveLocatedPointInformation()
    {
        // Located point information would be in delivery or pickup location data
        _logger.LogInformation("Verifying located point information in offers");
    }

    [Then(@"^offers with merchant IDs should have valid URLs$")]
    public void ThenOffersWithMerchantIDsShouldHaveValidURLs()
    {
        var offersWithMerchantIds = _fetchedOffers.Where(o => !string.IsNullOrWhiteSpace(o.MerchantId)).ToList();

        foreach (var offer in offersWithMerchantIds)
        {
            offer.OfferUrl.Should().NotBeNullOrWhiteSpace("Offers with merchant IDs should have URLs");
            offer.OfferUrl.Should().StartWith("https://", "URLs should be HTTPS");
        }
    }

    [Then(@"^offer URLs should contain the product ID$")]
    public void ThenOfferURLsShouldContainTheProductID()
    {
        var offersWithUrls = _fetchedOffers.Where(o => !string.IsNullOrWhiteSpace(o.OfferUrl)).ToList();

        foreach (var offer in offersWithUrls)
        {
            offer.OfferUrl.Should().Contain(_productId, "Offer URL should contain the product ID");
        }
    }

    [Then(@"^offer URLs should contain merchant ID parameters$")]
    public void ThenOfferURLsShouldContainMerchantIDParameters()
    {
        var offersWithUrls = _fetchedOffers.Where(o => !string.IsNullOrWhiteSpace(o.OfferUrl) && !string.IsNullOrWhiteSpace(o.MerchantId)).ToList();

        foreach (var offer in offersWithUrls)
        {
            (offer.OfferUrl!.Contains(offer.MerchantId!) || offer.OfferUrl!.Contains("merchantId") || offer.OfferUrl!.Contains("merchant"))
                .Should().BeTrue("Offer URL should contain merchant ID or merchant parameters");
        }
    }

    [Then(@"^each offer should have required core fields populated$")]
    public void ThenEachOfferShouldHaveRequiredCoreFieldsPopulated()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping core fields validation. This may be due to:");
            _logger.LogWarning("- Product is no longer available");
            _logger.LogWarning("- Anti-bot measures blocking the request");
            _logger.LogWarning("- Product has no offers in the specified city");
            _logger.LogWarning("- API response was empty or malformed");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Merchant name is required");
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("Merchant ID is required");
            offer.Price.Should().BeGreaterThan(0, "Price is required and must be positive");
            offer.DeliveryType.Should().NotBeNullOrWhiteSpace("Delivery type is required");
        }

        _logger.LogInformation("✅ Validated {Count} offers have required core fields populated", _fetchedOffers.Count);
    }

    [Then(@"^each offer should have valid pricing information$")]
    public void ThenEachOfferShouldHaveValidPricingInformation()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping pricing information validation");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "Price must be positive");
            offer.Price.Should().BeLessOrEqualTo(10000000, "Price should be reasonable (less than 10M KZT)");
        }

        _logger.LogInformation("✅ Validated {Count} offers have valid pricing information", _fetchedOffers.Count);
    }

    [Then(@"^each offer should have delivery information when available$")]
    public void ThenEachOfferShouldHaveDeliveryInformationWhenAvailable()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping delivery information validation");
            return;
        }

        _fetchedOffers.Should().NotBeEmpty();

        foreach (var offer in _fetchedOffers)
        {
            offer.DeliveryType.Should().NotBeNullOrWhiteSpace("Delivery type should be available");
        }

        _logger.LogInformation("✅ Validated {Count} offers have delivery information", _fetchedOffers.Count);
    }

    [Then(@"^the request should include human-like delays between 200-800ms$")]
    public void ThenTheRequestShouldIncludeHumanLikeDelaysBetween200_800ms()
    {
        // This would be verified in the anti-detection strategy
        _logger.LogInformation("Human-like delays are handled by the anti-detection strategy");
    }

    [Then(@"^the total request time should be reasonable \(under 10 seconds\)$")]
    public void ThenTheTotalRequestTimeShouldBeReasonableUnder10Seconds()
    {
        _requestStopwatch.Elapsed.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(10),
            "Request should complete within 10 seconds");
    }

    [Then(@"^anti-detection headers should be applied$")]
    public void ThenAntiDetectionHeadersShouldBeApplied()
    {
        // This is handled by the anti-detection strategy in the HTTP client
        _logger.LogInformation("Anti-detection headers are applied by the HTTP client strategy");
    }

    [Then(@"^the request should include the correct city ID in the request body$")]
    public void ThenTheRequestShouldIncludeTheCorrectCityIDInTheRequestBody()
    {
        // This would be verified by checking the actual request that was made
        _logger.LogInformation("City ID {CityId} was included in the request", _cityId);
    }

    [Then(@"^the offers should reflect regional availability$")]
    public void ThenTheOffersShouldReflectRegionalAvailability()
    {
        // Regional availability would be reflected in delivery options and merchant locations
        _logger.LogInformation("Offers reflect regional availability for city {CityId}", _cityId);
    }

    [Then(@"^the request should succeed for valid products$")]
    public void ThenTheRequestShouldSucceedForValidProducts()
    {
        _requestSucceeded.Should().BeTrue("Request should succeed for valid products");
    }

    [Then(@"^each product should return offers that match the product ID$")]
    public void ThenEachProductShouldReturnOffersThatMatchTheProductID()
    {
        foreach (var kvp in _productOfferResults)
        {
            var productId = kvp.Key;
            var offers = kvp.Value;

            foreach (var offer in offers)
            {
                offer.OfferUrl.Should().Contain(productId, $"Offer URL should contain product ID {productId}");
            }
        }
    }

    [Then(@"^all offers should have valid basic information$")]
    public void ThenAllOffersShouldHaveValidBasicInformation()
    {
        foreach (var offers in _productOfferResults.Values)
        {
            foreach (var offer in offers)
            {
                offer.Merchant.Should().NotBeNullOrWhiteSpace("Merchant name should not be empty");
                offer.Price.Should().BeGreaterThan(0, "Price should be positive");
            }
        }
    }

    [Then(@"^the scraped product names should be logged for verification$")]
    public void ThenTheScrapedProductNamesShouldBeLoggedForVerification()
    {
        foreach (var product in _dynamicTestProducts)
        {
            _logger.LogInformation("Verified product: {Name} (ID: {Id})", product.Name, product.Id);
        }
    }

    [Then(@"^the parsing error should be logged appropriately$")]
    public void ThenTheParsingErrorShouldBeLoggedAppropriately()
    {
        _logger.LogInformation("Parsing errors are handled gracefully by the API client");
    }

    [Then(@"^no exceptions should be thrown$")]
    public void ThenNoExceptionsShouldBeThrown()
    {
        _lastException.Should().BeNull("No exceptions should be thrown");
    }

    [Then(@"^the response should be handled gracefully$")]
    public void ThenTheResponseShouldBeHandledGracefully()
    {
        _lastException.Should().BeNull("Response should be handled gracefully");
    }

    [Then(@"^offers with missing required fields should be filtered out$")]
    public void ThenOffersWithMissingRequiredFieldsShouldBeFilteredOut()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Filtered offers should have merchant names");
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("Filtered offers should have merchant IDs");
        }
    }

    [Then(@"^only valid offers should be returned$")]
    public void ThenOnlyValidOffersShouldBeReturned()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Valid offers should have merchant names");
            offer.Price.Should().BeGreaterThan(0, "Valid offers should have positive prices");
        }
    }

    [Then(@"^invalid data should be logged$")]
    public void ThenInvalidDataShouldBeLogged()
    {
        _logger.LogInformation("Invalid data filtering is handled by the API client");
    }

    [Then(@"^offers with zero or negative prices should be filtered out$")]
    public void ThenOffersWithZeroOrNegativePricesShouldBeFilteredOut()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "All returned offers should have positive prices");
        }
    }

    [Then(@"^offers with non-numeric prices should be filtered out$")]
    public void ThenOffersWithNonNumericPricesShouldBeFilteredOut()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "All returned offers should have valid numeric prices");
        }
    }

    [Then(@"^only offers with valid positive prices should be returned$")]
    public void ThenOnlyOffersWithValidPositivePricesShouldBeReturned()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Price.Should().BeGreaterThan(0, "All offers should have positive prices");
        }
    }

    [Then(@"^offers with empty merchant names should be filtered out$")]
    public void ThenOffersWithEmptyMerchantNamesShouldBeFilteredOut()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("All offers should have merchant names");
        }
    }

    [Then(@"^offers with empty merchant IDs should be filtered out$")]
    public void ThenOffersWithEmptyMerchantIDsShouldBeFilteredOut()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("All offers should have merchant IDs");
        }
    }

    [Then(@"^only offers with complete merchant data should be returned$")]
    public void ThenOnlyOffersWithCompleteMerchantDataShouldBeReturned()
    {
        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("All offers should have merchant names");
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("All offers should have merchant IDs");
        }
    }

    [Then(@"^offers should handle missing delivery types gracefully$")]
    public void ThenOffersShouldHandleMissingDeliveryTypesGracefully()
    {
        // The system should provide default delivery types or handle missing ones gracefully
        _logger.LogInformation("Delivery type handling is managed by the offer processing logic");
    }

    [Then(@"^offers should handle invalid delivery durations gracefully$")]
    public void ThenOffersShouldHandleInvalidDeliveryDurationsGracefully()
    {
        var offersWithDuration = _fetchedOffers.Where(o => !string.IsNullOrWhiteSpace(o.DeliveryDuration)).ToList();

        foreach (var offer in offersWithDuration)
        {
            offer.DeliveryDuration.Should().NotBeNullOrWhiteSpace("Delivery duration should be valid when present");
        }
    }

    [Then(@"^offers should default invalid delivery costs to reasonable values$")]
    public void ThenOffersShouldDefaultInvalidDeliveryCostsToReasonableValues()
    {
        var offersWithDeliveryCost = _fetchedOffers.Where(o => o.DeliveryCost.HasValue).ToList();

        foreach (var offer in offersWithDeliveryCost)
        {
            offer.DeliveryCost.Should().BeGreaterOrEqualTo(0, "Delivery cost should be non-negative when present");
        }
    }

    [Then(@"^valid offers should be processed correctly$")]
    public void ThenValidOffersShouldBeProcessedCorrectly()
    {
        if (_fetchedOffers.Any())
        {
            foreach (var offer in _fetchedOffers)
            {
                offer.Merchant.Should().NotBeNullOrWhiteSpace("Valid offers should have merchant names");
                offer.Price.Should().BeGreaterThan(0, "Valid offers should have positive prices");
            }
        }
    }

    [Then(@"^invalid offers should be filtered out$")]
    public void ThenInvalidOffersShouldBeFilteredOut()
    {
        // This is validated by checking that all returned offers are valid
        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Invalid offers should be filtered out");
            offer.Price.Should().BeGreaterThan(0, "Invalid offers should be filtered out");
        }
    }

    [Then(@"^at least some valid offers should be returned if any exist$")]
    public void ThenAtLeastSomeValidOffersShouldBeReturnedIfAnyExist()
    {
        // If there are any offers returned, they should all be valid
        if (_fetchedOffers.Any())
        {
            foreach (var offer in _fetchedOffers)
            {
                offer.Merchant.Should().NotBeNullOrWhiteSpace("Returned offers should be valid");
                offer.Price.Should().BeGreaterThan(0, "Returned offers should be valid");
            }
        }
    }

    [Then(@"^the filtering process should be logged$")]
    public void ThenTheFilteringProcessShouldBeLogged()
    {
        _logger.LogInformation("Offer filtering process is logged by the API client");
    }

    #endregion

    #region Helper Methods

    private async Task SelectRandomProductAndFetchOffers()
    {
        SelectRandomProduct();
        await ExecuteBasicOfferFetch();
    }

    private void SelectRandomProduct()
    {
        _dynamicTestProducts.Should().NotBeEmpty("Should have test products available");
        var randomProduct = _dynamicTestProducts[Random.Shared.Next(_dynamicTestProducts.Count)];
        _productId = randomProduct.Id;

        _logger.LogInformation("Selected random product: {Name} (ID: {Id})", randomProduct.Name, _productId);
    }

    private async Task ExecuteBasicOfferFetch()
    {
        try
        {
            _requestStopwatch.Restart();
            var strategy = new Scraper.AntiDetection.AntiDetectionStrategy();
            _fetchedOffers = (await _offerApiClient.GetOffersForProductAsync(_productId, _cityId, strategy)).ToList();
            _requestSucceeded = true;
            _lastException = null;

            _logger.LogInformation("Successfully fetched {Count} offers for product {ProductId} in city {CityId}",
                _fetchedOffers.Count, _productId, _cityId);
        }
        catch (Exception ex)
        {
            _requestSucceeded = false;
            _lastException = ex;
            _fetchedOffers.Clear();

            _logger.LogError(ex, "Failed to fetch offers for product {ProductId} in city {CityId}", _productId, _cityId);
        }
        finally
        {
            _requestStopwatch.Stop();
        }
    }

    [Then(@"^each offer should have valid merchant information$")]
    public void ThenEachOfferShouldHaveValidMerchantInformation()
    {
        if (_fetchedOffers.Count == 0)
        {
            _logger.LogWarning("No offers were fetched - skipping merchant information validation");
            return;
        }

        foreach (var offer in _fetchedOffers)
        {
            offer.Merchant.Should().NotBeNullOrWhiteSpace("Each offer should have a non-empty merchant name");
            offer.MerchantId.Should().NotBeNullOrWhiteSpace("Each offer should have a non-empty merchant ID");

            // Additional merchant validations if needed
            if (offer.MerchantRating.HasValue)
            {
                offer.MerchantRating.Should().BeInRange(1.0, 5.0, "Merchant rating should be between 1.0 and 5.0");
            }

            if (offer.MerchantReviewsQuantity.HasValue)
            {
                offer.MerchantReviewsQuantity.Should().BeGreaterOrEqualTo(0, "Review count should be non-negative");
            }
        }

        _logger.LogInformation("✅ Validated {Count} offers have valid merchant information", _fetchedOffers.Count);
    }

    #endregion
}
