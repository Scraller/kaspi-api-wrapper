using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductSearchEngine.Scraper.Models
{
    /// <summary>
    /// Represents a category with hierarchical structure retrieved from Kaspi Navigation API.
    /// </summary>
    /// <param name="Name">The display name of the category (e.g., "Smartphones")</param>
    /// <param name="Slug">The URL-friendly identifier for the category (e.g., "smartfony")</param>
    /// <param name="Url">The complete URL to access this category on Kaspi</param>
    /// <param name="Level">The hierarchical level of this category (0 = top-level, 1 = first subcategory, etc.)</param>
    /// <param name="ParentSlug">The slug of the parent category (null for top-level categories)</param>
    public record HierarchicalCategoryInfo(
        /// <summary>
        /// The display name of the category
        /// </summary>
        /// <example>Smartphones</example>
        [Required]
        string Name,
        
        /// <summary>
        /// The URL-friendly identifier for the category
        /// </summary>
        /// <example>smartfony</example>
        [Required]
        string Slug,
        
        /// <summary>
        /// The complete URL to access this category on Kaspi
        /// </summary>
        /// <example>https://kaspi.kz/shop/c/smartfony/</example>
        [Required]
        string Url,
        
        /// <summary>
        /// The hierarchical level of this category (0 = top-level, 1 = first subcategory, etc.)
        /// </summary>
        /// <example>1</example>
        int Level,
        
        /// <summary>
        /// The slug of the parent category (null for top-level categories)
        /// </summary>
        /// <example>mobilnye-telefony</example>
        string? ParentSlug)
    {
        /// <summary>
        /// Direct subcategories of this category retrieved from the Navigation API.
        /// </summary>
        /// <example>[]</example>
        public List<HierarchicalCategoryInfo> Subcategories { get; init; } = new();
    }
}
