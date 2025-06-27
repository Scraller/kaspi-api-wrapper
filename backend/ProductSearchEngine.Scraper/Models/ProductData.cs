using System.Collections.Generic;

namespace ProductSearchEngine.Scraper.Models;

/// <summary>
/// Common data transfer object for product information extracted from marketplaces
/// </summary>
public class ProductData
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public bool InStock { get; set; }
    public int? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public Dictionary<string, string>? Specifications { get; set; }
}