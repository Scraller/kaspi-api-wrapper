using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Product listing response from discovered Kaspi API endpoints
/// Based on /yml/product-view/pl/results endpoint analysis
/// </summary>
public class ProductListingResponse
{
    /// <summary>
    /// List of products from the listing
    /// </summary>
    [Required]
    [JsonPropertyName("data")]
    public List<ProductItemResponse> Data { get; set; } = [];

    /// <summary>
    /// Promoted cards (if any)
    /// </summary>
    [JsonPropertyName("promotedCards")]
    public object? PromotedCards { get; set; }

    /// <summary>
    /// Promoted items (if any)
    /// </summary>
    [JsonPropertyName("promotedItems")]
    public object? PromotedItems { get; set; }
}

/// <summary>
/// Individual product item from product listing
/// Based on actual Kaspi API response structure
/// </summary>
public class ProductItemResponse
{
    /// <summary>
    /// Unique product identifier
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product title/name
    /// </summary>
    [Required]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Product brand
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Category identifier
    /// </summary>
    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// Whether product has variants
    /// </summary>
    [JsonPropertyName("hasVariants")]
    public bool HasVariants { get; set; }

    /// <summary>
    /// Whether loan is available
    /// </summary>
    [JsonPropertyName("loanAvailable")]
    public bool LoanAvailable { get; set; }

    /// <summary>
    /// Link to product page
    /// </summary>
    [JsonPropertyName("shopLink")]
    public string? ShopLink { get; set; }

    /// <summary>
    /// Unit price in tenge (smallest currency unit)
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Unit sale price in tenge (smallest currency unit)
    /// </summary>
    [JsonPropertyName("unitSalePrice")]
    public decimal UnitSalePrice { get; set; }

    /// <summary>
    /// Formatted price string
    /// </summary>
    [JsonPropertyName("priceFormatted")]
    public string? PriceFormatted { get; set; }

    /// <summary>
    /// Product creation timestamp
    /// </summary>
    [JsonPropertyName("createdTime")]
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    /// Product stickers/badges
    /// </summary>
    [JsonPropertyName("stickers")]
    public List<string> Stickers { get; set; } = [];

    /// <summary>
    /// Preview images for the product
    /// </summary>
    [JsonPropertyName("previewImages")]
    public List<ProductImageResponse> PreviewImages { get; set; } = [];

    /// <summary>
    /// Product teasers/promotional elements
    /// </summary>
    [JsonPropertyName("teasers")]
    public List<object> Teasers { get; set; } = [];

    /// <summary>
    /// Credit monthly price
    /// </summary>
    [JsonPropertyName("creditMonthlyPrice")]
    public decimal? CreditMonthlyPrice { get; set; }

    /// <summary>
    /// Monthly installment information
    /// </summary>
    [JsonPropertyName("monthlyInstallment")]
    public MonthlyInstallmentResponse? MonthlyInstallment { get; set; }

    /// <summary>
    /// Link to reviews
    /// </summary>
    [JsonPropertyName("reviewsLink")]
    public string? ReviewsLink { get; set; }

    /// <summary>
    /// Product weight
    /// </summary>
    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    /// <summary>
    /// Unit information
    /// </summary>
    [JsonPropertyName("unit")]
    public ProductUnitResponse? Unit { get; set; }

    /// <summary>
    /// Average rating
    /// </summary>
    [JsonPropertyName("rating")]
    public decimal Rating { get; set; }

    /// <summary>
    /// Number of reviews
    /// </summary>
    [JsonPropertyName("reviewsQuantity")]
    public int ReviewsQuantity { get; set; }

    /// <summary>
    /// Stock availability (1 = in stock, 0 = out of stock)
    /// </summary>
    [JsonPropertyName("stock")]
    public int Stock { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "KZT";

    /// <summary>
    /// Delivery duration
    /// </summary>
    [JsonPropertyName("deliveryDuration")]
    public string? DeliveryDuration { get; set; }

    /// <summary>
    /// Product categories
    /// </summary>
    [JsonPropertyName("category")]
    public List<string> Category { get; set; } = [];

    /// <summary>
    /// Product categories in Russian
    /// </summary>
    [JsonPropertyName("categoryRu")]
    public List<string> CategoryRu { get; set; } = [];

    /// <summary>
    /// Category codes
    /// </summary>
    [JsonPropertyName("categoryCodes")]
    public List<string> CategoryCodes { get; set; } = [];

    /// <summary>
    /// Promotional information
    /// </summary>
    [JsonPropertyName("promo")]
    public List<PromoResponse> Promo { get; set; } = [];

    /// <summary>
    /// Major merchants selling this product
    /// </summary>
    [JsonPropertyName("majorMerchants")]
    public List<string> MajorMerchants { get; set; } = [];

    /// <summary>
    /// Delivery zones where product is available
    /// </summary>
    [JsonPropertyName("deliveryZones")]
    public List<string> DeliveryZones { get; set; } = [];
}

/// <summary>
/// Product image response with different sizes
/// </summary>
public class ProductImageResponse
{
    /// <summary>
    /// Small image URL
    /// </summary>
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    /// <summary>
    /// Medium image URL
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Large image URL
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

/// <summary>
/// Monthly installment information
/// </summary>
public class MonthlyInstallmentResponse
{
    /// <summary>
    /// Installment identifier
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Whether installment is available
    /// </summary>
    [JsonPropertyName("installment")]
    public bool Installment { get; set; }

    /// <summary>
    /// Formatted monthly payment amount
    /// </summary>
    [JsonPropertyName("formattedPerMonth")]
    public string? FormattedPerMonth { get; set; }
}

/// <summary>
/// Product unit information
/// </summary>
public class ProductUnitResponse
{
    /// <summary>
    /// Unit type (PIECES, MEASURABLE, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Unit increment
    /// </summary>
    [JsonPropertyName("increment")]
    public decimal Increment { get; set; }

    /// <summary>
    /// Measurement literal (шт, кг, etc.)
    /// </summary>
    [JsonPropertyName("measurementLiteral")]
    public string? MeasurementLiteral { get; set; }

    /// <summary>
    /// Counting literal
    /// </summary>
    [JsonPropertyName("countingLiteral")]
    public string? CountingLiteral { get; set; }
}

/// <summary>
/// Promotional information
/// </summary>
public class PromoResponse
{
    /// <summary>
    /// Promo code
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Promo text
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Promo type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Priority
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Owner
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Bonus type
    /// </summary>
    [JsonPropertyName("bonusType")]
    public string? BonusType { get; set; }
}

/// <summary>
/// Product reviews response structure
/// Based on /yml/review-view/api/v1/reviews/product/{id} endpoint analysis
/// </summary>
public class ProductReviewsResponse
{
    /// <summary>
    /// List of product reviews
    /// </summary>
    [JsonPropertyName("data")]
    public List<ProductReviewResponse> Data { get; set; } = [];

    /// <summary>
    /// Review summary statistics
    /// </summary>
    [JsonPropertyName("summary")]
    public ReviewSummaryResponse? Summary { get; set; }

    /// <summary>
    /// Group summary statistics
    /// </summary>
    [JsonPropertyName("groupSummary")]
    public List<ReviewGroupSummaryResponse> GroupSummary { get; set; } = [];

    /// <summary>
    /// Total count of images in reviews
    /// </summary>
    [JsonPropertyName("imagesSummaryCount")]
    public int ImagesSummaryCount { get; set; }
}

/// <summary>
/// Individual product review
/// </summary>
public class ProductReviewResponse
{
    /// <summary>
    /// Review identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Review author name
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Review date
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// Order number
    /// </summary>
    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Rating (1-5)
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// Review comment
    /// </summary>
    [JsonPropertyName("comment")]
    public ReviewCommentResponse? Comment { get; set; }

    /// <summary>
    /// Review feedback (likes, etc.)
    /// </summary>
    [JsonPropertyName("feedback")]
    public ReviewFeedbackResponse? Feedback { get; set; }

    /// <summary>
    /// Gallery images attached to review
    /// </summary>
    [JsonPropertyName("galleryImages")]
    public List<ReviewImageResponse> GalleryImages { get; set; } = [];

    /// <summary>
    /// Product information
    /// </summary>
    [JsonPropertyName("product")]
    public ReviewProductResponse? Product { get; set; }

    /// <summary>
    /// Merchant information (if applicable)
    /// </summary>
    [JsonPropertyName("merchant")]
    public ReviewMerchantResponse? Merchant { get; set; }

    /// <summary>
    /// Whether review is editable
    /// </summary>
    [JsonPropertyName("editable")]
    public bool Editable { get; set; }

    /// <summary>
    /// Whether review was edited by customer
    /// </summary>
    [JsonPropertyName("editedByCustomer")]
    public bool EditedByCustomer { get; set; }
}

/// <summary>
/// Review comment structure
/// </summary>
public class ReviewCommentResponse
{
    /// <summary>
    /// Negative aspects
    /// </summary>
    [JsonPropertyName("minus")]
    public string? Minus { get; set; }

    /// <summary>
    /// Positive aspects
    /// </summary>
    [JsonPropertyName("plus")]
    public string? Plus { get; set; }

    /// <summary>
    /// Review text
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Review feedback information
/// </summary>
public class ReviewFeedbackResponse
{
    /// <summary>
    /// Number of positive votes
    /// </summary>
    [JsonPropertyName("positive")]
    public int Positive { get; set; }

    /// <summary>
    /// Whether user has voted
    /// </summary>
    [JsonPropertyName("voted")]
    public bool Voted { get; set; }
}

/// <summary>
/// Review image with different sizes
/// </summary>
public class ReviewImageResponse
{
    /// <summary>
    /// Image identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Small image URL
    /// </summary>
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    /// <summary>
    /// Medium image URL
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Large image URL
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

/// <summary>
/// Product information in review
/// </summary>
public class ReviewProductResponse
{
    /// <summary>
    /// Product identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Category code
    /// </summary>
    [JsonPropertyName("categoryCode")]
    public string? CategoryCode { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// Product link
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }
}

/// <summary>
/// Merchant information in review
/// </summary>
public class ReviewMerchantResponse
{
    /// <summary>
    /// Merchant name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Merchant code
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Review summary statistics
/// </summary>
public class ReviewSummaryResponse
{
    /// <summary>
    /// Overall rating
    /// </summary>
    [JsonPropertyName("global")]
    public decimal Global { get; set; }

    /// <summary>
    /// Rating distribution statistics
    /// </summary>
    [JsonPropertyName("statistic")]
    public List<RatingStatisticResponse> Statistic { get; set; } = [];
}

/// <summary>
/// Rating statistic for specific rating value
/// </summary>
public class RatingStatisticResponse
{
    /// <summary>
    /// Rating value (1-5)
    /// </summary>
    [JsonPropertyName("rate")]
    public int Rate { get; set; }

    /// <summary>
    /// Count of reviews with this rating
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Review group summary
/// </summary>
public class ReviewGroupSummaryResponse
{
    /// <summary>
    /// Group identifier (ALL, COMMENT, PICTURE, etc.)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Total count in this group
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}
