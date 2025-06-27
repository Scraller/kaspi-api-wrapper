using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Models;

/// <summary>
/// Enhanced city response model based on real Kaspi.kz data
/// </summary>
public class CityResponse
{
    /// <summary>
    /// Kaspi internal city ID
    /// </summary>
    /// <example>750000000</example>
    public required string Id { get; set; }

    /// <summary>
    /// City name in local language (Russian/Kazakh)
    /// </summary>
    /// <example>Алматы</example>
    public required string Name { get; set; }

    /// <summary>
    /// English name of the city
    /// </summary>
    /// <example>Almaty</example>
    public required string NameEn { get; set; }

    /// <summary>
    /// URL-friendly slug used by Kaspi
    /// </summary>
    /// <example>almaty</example>
    public required string Slug { get; set; }

    /// <summary>
    /// Whether this is a major regional center
    /// </summary>
    /// <example>true</example>
    public bool IsMajorCity { get; set; }

    /// <summary>
    /// Whether services are available in this city
    /// </summary>
    /// <example>true</example>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Direct Kaspi URL for this city
    /// </summary>
    /// <example>https://kaspi.kz/shop/almaty/</example>
    public required string KaspiUrl { get; set; }
}

/// <summary>
/// Regional availability response
/// </summary>
public class RegionalAvailabilityResponse
{
    /// <summary>
    /// City ID
    /// </summary>
    /// <example>750000000</example>
    public required string CityId { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    /// <example>Алматы</example>
    public required string CityName { get; set; }

    /// <summary>
    /// City name in English
    /// </summary>
    /// <example>Almaty</example>
    public string? CityNameEn { get; set; }

    /// <summary>
    /// Whether services are available in this region
    /// </summary>
    /// <example>true</example>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Product ID being checked (if specified)
    /// </summary>
    /// <example>101349284</example>
    public string? ProductId { get; set; }

    /// <summary>
    /// Total number of offers for the product (if specified)
    /// </summary>
    /// <example>5</example>
    public int? OfferCount { get; set; }

    /// <summary>
    /// Number of in-stock offers for the product (if specified)
    /// </summary>
    /// <example>3</example>
    public int? InStockOfferCount { get; set; }

    /// <summary>
    /// Minimum price among all offers (if specified)
    /// </summary>
    /// <example>150000</example>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price among all offers (if specified)
    /// </summary>
    /// <example>200000</example>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Currency for prices
    /// </summary>
    /// <example>KZT</example>
    public string? Currency { get; set; }

    /// <summary>
    /// Available delivery options
    /// </summary>
    /// <example>["Доставка", "Самовывоз", "Экспресс-доставка"]</example>
    public required List<string> DeliveryOptions { get; set; }

    /// <summary>
    /// Supported payment methods
    /// </summary>
    /// <example>["Kaspi Red", "Kaspi Gold", "Рассрочка"]</example>
    public required List<string> PaymentMethods { get; set; }

    /// <summary>
    /// Special offers available in this region
    /// </summary>
    /// <example>["Бесплатная доставка от 15000 тг", "Кешбэк до 10%"]</example>
    public required List<string> SpecialOffers { get; set; }

    /// <summary>
    /// When the availability was last checked
    /// </summary>
    /// <example>2025-06-26T10:30:00Z</example>
    public DateTime? LastChecked { get; set; }
}

/// <summary>
/// Request model for searching cities
/// </summary>
public class SearchCitiesRequest
{
    /// <summary>
    /// Search term for city name (partial matches supported)
    /// </summary>
    /// <example>Алма</example>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Whether to return only major cities
    /// </summary>
    /// <example>false</example>
    public bool MajorCitiesOnly { get; set; } = false;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    /// <example>20</example>
    [Range(1, 100)]
    public int MaxResults { get; set; } = 50;
}

/// <summary>
/// Request model for checking regional availability
/// </summary>
public class RegionalAvailabilityRequest
{
    /// <summary>
    /// City ID to check availability for
    /// </summary>
    /// <example>750000000</example>
    [Required]
    public required string CityId { get; set; }

    /// <summary>
    /// Optional product ID to check specific product availability
    /// </summary>
    /// <example>102298404</example>
    public string? ProductId { get; set; }

    /// <summary>
    /// Optional merchant ID to check merchant coverage
    /// </summary>
    /// <example>KolaNur</example>
    public string? MerchantId { get; set; }
}
