namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Domain model for a product entity
/// </summary>
public class Product
{
    /// <summary>
    /// Unique product identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly product slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Product title from Kaspi page
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Complete product description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gallery images from product page
    /// </summary>
    public List<string> GalleryImages { get; set; } = [];
    
    /// <summary>
    /// Minimum price across all offers
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// Current price
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// Currency code (KZT)
    /// </summary>
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
    /// Marketplace name
    /// </summary>
    public string? Marketplace { get; set; }
    
    /// <summary>
    /// Category information
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Category information
    /// </summary>
    public string? CategoryId { get; set; }
    
    /// <summary>
    /// Category name
    /// </summary>
    public string? CategoryName { get; set; }
    
    /// <summary>
    /// Product URL
    /// </summary>
    public string? ProductUrl { get; set; }
    
    /// <summary>
    /// Main image URL
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Product brand
    /// </summary>
    public string? Brand { get; set; }
    
    /// <summary>
    /// Product model
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Product rating
    /// </summary>
    public int? Rating { get; set; }
    
    /// <summary>
    /// Review count
    /// </summary>
    public int? ReviewCount { get; set; }
    
    /// <summary>
    /// In stock status
    /// </summary>
    public bool InStock { get; set; } = true;
    
    /// <summary>
    /// Product specifications
    /// </summary>
    public Dictionary<string, object>? Specifications { get; set; }
    
    /// <summary>
    /// Additional product attributes and metadata
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = [];
    
    /// <summary>
    /// Product creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last updated (legacy property)
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
