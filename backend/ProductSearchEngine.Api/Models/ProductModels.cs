using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Models;

/// <summary>
/// Detailed product information response model
/// </summary>
public class ProductDetailResponse
{
    /// <summary>
    /// Unique product identifier
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Product name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly product slug
    /// </summary>
    [Required] 
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Product title from Kaspi page
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Complete product description extracted from BACKEND.components.item.description
    /// </summary>
    /// <example>Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени...</example>
    public string? Description { get; set; }
    
    /// <summary>
    /// Grouped product specifications extracted from BACKEND.components.item.specifications
    /// Contains hierarchical specification data organized by feature groups
    /// </summary>
    public List<SpecificationGroup> Specifications { get; set; } = [];
    
    /// <summary>
    /// Gallery images from product page
    /// </summary>
    public List<string> GalleryImages { get; set; } = [];
    
    /// <summary>
    /// Minimum price across all offers
    /// </summary>
    [Required]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Currency code (KZT)
    /// </summary>
    [Required]
    public string Currency { get; set; } = "KZT";
    
    /// <summary>
    /// City code for regional pricing
    /// </summary>
    public string? CityCode { get; set; }
    
    /// <summary>
    /// City name for display
    /// </summary>
    public string? CityName { get; set; }
    
    /// <summary>
    /// Available offers from different merchants
    /// </summary>
    public List<OfferResponse> Offers { get; set; } = [];
    
    /// <summary>
    /// Additional product attributes and metadata
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = [];
}

/// <summary>
/// Product offer information from a specific merchant
/// </summary>
public class OfferResponse
{
    /// <summary>
    /// Merchant identifier
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant name
    /// </summary>
    [Required]
    public string MerchantName { get; set; } = string.Empty;
    
    /// <summary>
    /// Offer price
    /// </summary>
    [Required]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    [Required]
    public string Currency { get; set; } = "KZT";
    
    /// <summary>
    /// Product availability status
    /// </summary>
    [Required]
    public string Availability { get; set; } = string.Empty;
    
    /// <summary>
    /// Delivery information
    /// </summary>
    public string? DeliveryInfo { get; set; }
    
    /// <summary>
    /// Merchant rating (1.0 - 5.0)
    /// </summary>
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Number of customer reviews
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// Direct link to the product offer
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Product search response containing results and pagination
/// </summary>
public class ProductSearchResponse
{
    /// <summary>
    /// List of products matching the search criteria
    /// </summary>
    [Required]
    public List<ProductSummaryResponse> Products { get; set; } = [];
    
    /// <summary>
    /// Total number of products found
    /// </summary>
    [Required]
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    [Required]
    public int Page { get; set; }
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    [Required]
    public int PageSize { get; set; }
    
    /// <summary>
    /// Whether there are more pages available
    /// </summary>
    [Required]
    public bool HasNextPage { get; set; }
    
    /// <summary>
    /// Original search query
    /// </summary>
    public string? SearchQuery { get; set; }
    
    /// <summary>
    /// Category filter applied
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Summary product information for search results
/// </summary>
public class ProductSummaryResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Product name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly product slug
    /// </summary>
    [Required]
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Product price
    /// </summary>
    [Required]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    [Required]
    public string Currency { get; set; } = "KZT";
    
    /// <summary>
    /// Product image URL
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Average rating
    /// </summary>
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Number of reviews
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// Availability status
    /// </summary>
    [Required]
    public string Availability { get; set; } = string.Empty;
    
    /// <summary>
    /// Product category
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Advanced search request model with comprehensive filtering
/// </summary>
public class AdvancedSearchRequest
{
    /// <summary>
    /// Search query text (minimum 2 characters)
    /// </summary>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Category filter (category slug)
    /// </summary>
    [StringLength(50)]
    public string? Category { get; set; }
    
    /// <summary>
    /// Merchant name filter
    /// </summary>
    [StringLength(50)]
    public string? MerchantName { get; set; }
    
    /// <summary>
    /// Price range filter (e.g., "10000-50000")
    /// </summary>
    [StringLength(20)]
    public string? PriceRange { get; set; }
    
    /// <summary>
    /// City code for regional pricing (default: 750000000 - Almaty)
    /// </summary>
    [StringLength(20)]
    public string? CityCode { get; set; } = "750000000";
    
    /// <summary>
    /// Page number for pagination (0-based)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Page { get; set; } = 0;
    
    /// <summary>
    /// Number of items per page (1-100)
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Sort options (e.g., "price_asc", "rating_desc", "popularity")
    /// </summary>
    public List<string> SortOptions { get; set; } = new();
    
    /// <summary>
    /// Additional filters as key-value pairs
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = new();
}

/// <summary>
/// Search suggestion response model
/// </summary>
public class SearchSuggestionResponse
{
    /// <summary>
    /// Suggested search text
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of suggestion (product, category, brand)
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Related category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Popularity score (0.0 - 1.0)
    /// </summary>
    public float PopularityScore { get; set; }
}

/// <summary>
/// Search filters response model
/// </summary>
public class SearchFiltersResponse
{
    /// <summary>
    /// Category these filters apply to
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Available filter groups
    /// </summary>
    [Required]
    public List<FilterGroup> FilterGroups { get; set; } = new();
}

/// <summary>
/// Filter group containing related filter options
/// </summary>
public class FilterGroup
{
    /// <summary>
    /// Display name of the filter group
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique key for the filter group
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Available filter options
    /// </summary>
    [Required]
    public List<FilterOption> Options { get; set; } = new();
}

/// <summary>
/// Individual filter option within a filter group
/// </summary>
public class FilterOption
{
    /// <summary>
    /// Display label for the filter option
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Value to use in filter queries
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of products matching this filter
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Product specification group containing related features
/// Based on BACKEND.components.item.specifications structure
/// </summary>
public class SpecificationGroup
{
    /// <summary>
    /// Unique code identifier for the specification group
    /// </summary>
    /// <example>Meat grinders*Features</example>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name of the specification group
    /// </summary>
    /// <example>Особенности</example>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// List of features within this specification group
    /// </summary>
    [Required]
    public List<SpecificationFeature> Features { get; set; } = [];
}

/// <summary>
/// Individual product specification feature
/// Represents a single product characteristic with its values
/// </summary>
public class SpecificationFeature
{
    /// <summary>
    /// Unique code identifier for the feature
    /// </summary>
    /// <example>meat grinders*tray material</example>
    public string? Code { get; set; }
    
    /// <summary>
    /// Display name of the feature
    /// </summary>
    /// <example>Материал лотка</example>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the feature (ENUM, STRING, NUMBER, etc.)
    /// </summary>
    /// <example>ENUM</example>
    public string? Type { get; set; }
    
    /// <summary>
    /// List of feature values
    /// </summary>
    [Required]
    public List<SpecificationFeatureValue> FeatureValues { get; set; } = [];
    
    /// <summary>
    /// Position/order of the feature within the group
    /// </summary>
    public int? Position { get; set; }
    
    /// <summary>
    /// Whether this feature is visible to users
    /// </summary>
    public bool Visible { get; set; } = true;
    
    /// <summary>
    /// Whether this feature can have multiple values
    /// </summary>
    public bool? MultiValued { get; set; }
}

/// <summary>
/// Individual value for a product specification feature
/// </summary>
public class SpecificationFeatureValue
{
    /// <summary>
    /// The actual value of the feature
    /// </summary>
    /// <example>металл</example>
    [Required]
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata for the value (optional)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Product description response model
/// Contains the complete product description extracted from BACKEND.components.item.description
/// </summary>
public class ProductDescriptionResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    /// <example>129349158</example>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Complete product description text
    /// </summary>
    /// <example>Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени...</example>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Length of the description text in characters
    /// </summary>
    /// <example>1250</example>
    public int Length { get; set; }

    /// <summary>
    /// Whether the product has meaningful description content
    /// </summary>
    /// <example>true</example>
    public bool HasContent { get; set; }

    /// <summary>
    /// City code used for extraction
    /// </summary>
    /// <example>750000000</example>
    public string? CityCode { get; set; }
}

/// <summary>
/// Product specifications response model
/// Contains hierarchical product specifications organized by groups
/// </summary>
public class ProductSpecificationsResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    /// <example>129349158</example>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of specification groups
    /// </summary>
    /// <example>4</example>
    public int TotalGroups { get; set; }

    /// <summary>
    /// Total number of individual features across all groups
    /// </summary>
    /// <example>12</example>
    public int TotalFeatures { get; set; }

    /// <summary>
    /// Hierarchical specification groups (Характеристики, Особенности, etc.)
    /// </summary>
    [Required]
    public List<SpecificationGroup> SpecificationGroups { get; set; } = [];

    /// <summary>
    /// City code used for extraction
    /// </summary>
    /// <example>750000000</example>
    public string? CityCode { get; set; }
}

/// <summary>
/// Product gallery response model
/// Contains product images with multiple resolution options
/// </summary>
public class ProductGalleryResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    /// <example>129349158</example>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of gallery images
    /// </summary>
    /// <example>5</example>
    public int TotalImages { get; set; }

    /// <summary>
    /// Gallery images with multiple resolution URLs
    /// </summary>
    [Required]
    public List<ProductImageResponse> Images { get; set; } = [];

    /// <summary>
    /// City code used for extraction
    /// </summary>
    /// <example>750000000</example>
    public string? CityCode { get; set; }
}

/// <summary>
/// Individual product image with multiple resolution URLs
/// </summary>
public class ProductImageResponse
{
    /// <summary>
    /// Small resolution image URL
    /// </summary>
    /// <example>https://kaspi.kz/img/m/p/h123/small.jpg</example>
    [Required]
    public string SmallUrl { get; set; } = string.Empty;

    /// <summary>
    /// Medium resolution image URL
    /// </summary>
    /// <example>https://kaspi.kz/img/m/p/h123/medium.jpg</example>
    [Required]
    public string MediumUrl { get; set; } = string.Empty;

    /// <summary>
    /// Large resolution image URL
    /// </summary>
    /// <example>https://kaspi.kz/img/m/p/h123/large.jpg</example>
    [Required]
    public string LargeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Image location identifier
    /// </summary>
    /// <example>main</example>
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Complete product information response model
/// Contains description, specifications, and gallery images in a single response
/// </summary>
public class ProductCompleteResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    /// <example>129349158</example>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product title from Kaspi page
    /// </summary>
    /// <example>Мясорубка электрическая Zepter ZP-987 белый</example>
    public string? Title { get; set; }

    /// <summary>
    /// Complete product description
    /// </summary>
    /// <example>Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени...</example>
    public string? Description { get; set; }

    /// <summary>
    /// Hierarchical specification groups
    /// </summary>
    [Required]
    public List<SpecificationGroup> SpecificationGroups { get; set; } = [];

    /// <summary>
    /// Total number of specification groups
    /// </summary>
    /// <example>4</example>
    public int TotalSpecificationGroups { get; set; }

    /// <summary>
    /// Total number of individual features across all groups
    /// </summary>
    /// <example>12</example>
    public int TotalSpecificationFeatures { get; set; }

    /// <summary>
    /// Gallery images with multiple resolution URLs
    /// </summary>
    [Required]
    public List<ProductImageResponse> GalleryImages { get; set; } = [];

    /// <summary>
    /// Total number of gallery images
    /// </summary>
    /// <example>5</example>
    public int TotalImages { get; set; }

    /// <summary>
    /// City code used for extraction
    /// </summary>
    /// <example>750000000</example>
    public string? CityCode { get; set; }

    /// <summary>
    /// Whether the product has a meaningful description
    /// </summary>
    /// <example>true</example>
    public bool HasDescription { get; set; }

    /// <summary>
    /// Whether the product has specifications data
    /// </summary>
    /// <example>true</example>
    public bool HasSpecifications { get; set; }

    /// <summary>
    /// Whether the product has gallery images
    /// </summary>
    /// <example>true</example>
    public bool HasGallery { get; set; }
}

/// <summary>
/// Product metadata response model
/// Contains information about what data is available for a product without full extraction
/// </summary>
public class ProductMetadataResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    /// <example>129349158</example>
    [Required]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product title from Kaspi page
    /// </summary>
    /// <example>Мясорубка электрическая Zepter ZP-987 белый</example>
    public string? Title { get; set; }

    /// <summary>
    /// Whether the product has a description available
    /// </summary>
    /// <example>true</example>
    public bool HasDescription { get; set; }

    /// <summary>
    /// Whether the product has specifications available
    /// </summary>
    /// <example>true</example>
    public bool HasSpecifications { get; set; }

    /// <summary>
    /// Whether the product has gallery images available
    /// </summary>
    /// <example>true</example>
    public bool HasGallery { get; set; }

    /// <summary>
    /// Number of specification groups available
    /// </summary>
    /// <example>4</example>
    public int SpecificationGroupCount { get; set; }

    /// <summary>
    /// Number of gallery images available
    /// </summary>
    /// <example>5</example>
    public int ImageCount { get; set; }

    /// <summary>
    /// Overall data quality assessment
    /// </summary>
    /// <example>complete</example>
    public string DataQuality { get; set; } = "unknown";

    /// <summary>
    /// City code used for extraction
    /// </summary>
    /// <example>750000000</example>
    public string? CityCode { get; set; }
}
