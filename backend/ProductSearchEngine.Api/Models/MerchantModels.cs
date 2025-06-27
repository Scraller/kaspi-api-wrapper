using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Api.Models;

/// <summary>
/// Detailed merchant information response model based on Phase 2 discovery findings.
/// Maps data from BACKEND.components.merchant object structure.
/// </summary>
public class MerchantDetailResponse
{
    /// <summary>
    /// Unique merchant identifier (from BACKEND.components.merchant.uid)
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Merchant business name (from BACKEND.components.merchant.name)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Business description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Customer rating (1.0 - 5.0) from BACKEND.components.merchant.rating
    /// </summary>
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Total number of customer reviews from BACKEND.components.merchant.numberOfReviews
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// Number of products in catalog (mapped from BACKEND.components.merchant.salesCount)
    /// Phase 2 discovery: salesCount is always present and represents total sales
    /// </summary>
    [Required]
    public int ProductCount { get; set; }
    
    /// <summary>
    /// Whether the merchant is verified (merchants with BACKEND.components.merchant data are verified)
    /// </summary>
    [Required]
    public bool IsVerified { get; set; }
    
    /// <summary>
    /// Merchant contact information (extracted from BACKEND.components.merchant)
    /// </summary>
    public MerchantContactInfo? ContactInfo { get; set; }
    
    /// <summary>
    /// Product categories offered by merchant
    /// </summary>
    public List<string> Categories { get; set; } = [];
    
    /// <summary>
    /// Operational information and policies
    /// </summary>
    public MerchantOperationalInfo? OperationalInfo { get; set; }
    
    /// <summary>
    /// Merchant establishment date (from BACKEND.components.merchant.create)
    /// </summary>
    public DateTime? EstablishedDate { get; set; }
    
    /// <summary>
    /// Logo URL (from BACKEND.components.merchant.logo, can be null)
    /// </summary>
    public string? LogoUrl { get; set; }
}

/// <summary>
/// Merchant contact information extracted from BACKEND.components.merchant
/// </summary>
public class MerchantContactInfo
{
    /// <summary>
    /// Business website URL
    /// </summary>
    public string? Website { get; set; }
    
    /// <summary>
    /// Contact phone number (from BACKEND.components.merchant.phone)
    /// Phase 2 discovery: Various formats preserved (e.g., "3210", "+7 (708) 028-31-30")
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Contact email address
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Business address
    /// </summary>
    public string? Address { get; set; }
}

/// <summary>
/// Merchant operational information
/// </summary>
public class MerchantOperationalInfo
{
    /// <summary>
    /// Whether the merchant is currently active
    /// </summary>
    [Required]
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Available delivery options
    /// </summary>
    public List<string> DeliveryOptions { get; set; } = [];
    
    /// <summary>
    /// Accepted payment methods
    /// </summary>
    public List<string> PaymentMethods { get; set; } = [];
    
    /// <summary>
    /// Working hours information
    /// </summary>
    public string? WorkingHours { get; set; }
}

/// <summary>
/// Merchant summary information for search results
/// </summary>
public class MerchantResponse
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
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Business description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Customer rating
    /// </summary>
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Number of reviews
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// Whether the merchant is verified
    /// </summary>
    [Required]
    public bool IsVerified { get; set; }
    
    /// <summary>
    /// Number of products available
    /// </summary>
    [Required]
    public int ProductCount { get; set; }
    
    /// <summary>
    /// Main product categories
    /// </summary>
    public List<string> Categories { get; set; } = [];
}
/// <summary>
/// Response model for merchant search results
/// </summary>
public class MerchantSearchResponse
{
    /// <summary>
    /// List of merchants matching the search query
    /// </summary>
    [Required]
    public List<MerchantResponse> Merchants { get; set; } = [];
    
    /// <summary>
    /// Total number of merchants found
    /// </summary>
    [Required]
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Search query that was executed
    /// </summary>
    public string? Query { get; set; }
}
