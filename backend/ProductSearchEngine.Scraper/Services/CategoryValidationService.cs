using System;
using System.Linq;
using ProductSearchEngine.Scraper.Configuration;

namespace ProductSearchEngine.Scraper.Services
{
    /// <summary>
    /// Service responsible for validating category links and filtering unwanted categories.
    /// </summary>
    public class CategoryValidationService
    {
        /// <summary>
        /// Validates if a category link should be included in the extraction.
        /// </summary>
        /// <param name="href">URL of the category</param>
        /// <param name="text">Display text of the category</param>
        /// <returns>True if the category should be included, false otherwise</returns>
        public bool IsValidCategoryLink(string href, string text)
        {
            if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(text))
                return false;

            if (href.Contains("mailto:") || href.Contains("tel:") || href.StartsWith("#"))
                return false;

            // Filter out excluded URL patterns
            if (KaspiCategoryHierarchyConfig.ExcludedUrlPatterns.Any(pattern => href.Contains(pattern)))
                return false;

            // Filter out excluded category names
            if (KaspiCategoryHierarchyConfig.ExcludedCategoryNames.Contains(text))
                return false;

            // Filter out URLs that point to the categories summary page
            if (href.Contains("/categories/") && !href.Contains("/c/"))
                return false;

            // We want shop category links
            return href.Contains("/shop/c/") && text.Length > 2;
        }

        /// <summary>
        /// Extracts the category slug from a Kaspi category URL.
        /// </summary>
        /// <param name="url">Full category URL</param>
        /// <returns>Category slug or empty string if not found</returns>
        public string ExtractSlugFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;

                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i].TrimEnd('/') == "c" && i + 1 < segments.Length)
                    {
                        return segments[i + 1].TrimEnd('/');
                    }
                }
            }
            catch (UriFormatException)
            {
                // Invalid URL format
            }

            return string.Empty;
        }
    }
}
