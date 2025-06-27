namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Domain model for a product offer from a specific merchant
/// </summary>
public class Offer
{
    /// <summary>
    /// Offer identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated product identifier
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant identifier
    /// </summary>
    public string MerchantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant name
    /// </summary>
    public string MerchantName { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant name (legacy property)
    /// </summary>
    public string Merchant { get; set; } = string.Empty;
    
    /// <summary>
    /// Offer price
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Currency code (KZT)
    /// </summary>
    public string Currency { get; set; } = "KZT";
    
    /// <summary>
    /// Availability status
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// In stock status
    /// </summary>
    public bool InStock { get; set; } = true;
    
    /// <summary>
    /// Stock quantity if available
    /// </summary>
    public int? StockQuantity { get; set; }
    
    /// <summary>
    /// Master SKU
    /// </summary>
    public string? MasterSku { get; set; }
    
    /// <summary>
    /// Merchant SKU
    /// </summary>
    public string? MerchantSku { get; set; }
    
    /// <summary>
    /// Number of merchant reviews
    /// </summary>
    public int? MerchantReviewsQuantity { get; set; }
    
    /// <summary>
    /// Delivery options
    /// </summary>
    public List<string> DeliveryOptions { get; set; } = [];
    
    /// <summary>
    /// Payment options
    /// </summary>
    public List<string> PaymentOptions { get; set; } = [];
    
    /// <summary>
    /// Merchant rating
    /// </summary>
    public double? MerchantRating { get; set; }
    
    /// <summary>
    /// Number of reviews for this merchant
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// Delivery date
    /// </summary>
    public DateTime? DeliveryDate { get; set; }
    
    /// <summary>
    /// Pickup date
    /// </summary>
    public DateTime? PickupDate { get; set; }
    
    /// <summary>
    /// Delivery type
    /// </summary>
    public string? DeliveryType { get; set; }
    
    /// <summary>
    /// Delivery duration
    /// </summary>
    public string? DeliveryDuration { get; set; }
    
    /// <summary>
    /// Kaspi delivery available
    /// </summary>
    public bool? KaspiDelivery { get; set; }
    
    /// <summary>
    /// Inter-city delivery available
    /// </summary>
    public bool? InterCity { get; set; }
    
    /// <summary>
    /// Delivery cost
    /// </summary>
    public decimal? DeliveryCost { get; set; }
    
    /// <summary>
    /// Delivery threshold for free delivery
    /// </summary>
    public decimal? DeliveryThreshold { get; set; }
    
    /// <summary>
    /// Availability date
    /// </summary>
    public DateTime? AvailabilityDate { get; set; }
    
    /// <summary>
    /// Preorder status
    /// </summary>
    public int? Preorder { get; set; }
    
    /// <summary>
    /// Located in point
    /// </summary>
    public string? LocatedInPoint { get; set; }
    
    /// <summary>
    /// Kaspi delivery points
    /// </summary>
    public List<string> KdPoints { get; set; } = [];
    
    /// <summary>
    /// Offer URL
    /// </summary>
    public string? OfferUrl { get; set; }
    
    /// <summary>
    /// City code for regional availability
    /// </summary>
    public string? CityCode { get; set; }
    
    /// <summary>
    /// City name for display
    /// </summary>
    public string? CityName { get; set; }
    
    /// <summary>
    /// Additional offer attributes
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = [];
    
    /// <summary>
    /// Offer creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp (legacy property)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last updated (legacy property)
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
