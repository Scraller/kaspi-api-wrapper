using System.Collections.Generic;
using System.Linq;
using ProductSearchEngine.Scraper.Configuration;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Services
{
    /// <summary>
    /// Service responsible for building category hierarchy from flat category data.
    /// </summary>
    public class CategoryHierarchyBuilder
    {
        /// <summary>
        /// Builds a hierarchical category structure from flat category data.
        /// </summary>
        /// <param name="categories">Flat list of categories with basic information</param>
        /// <returns>List of categories with proper hierarchy relationships</returns>
        public List<HierarchicalCategoryInfo> BuildHierarchy(List<(string name, string url, string slug, int productCount)> categories)
        {
            var result = new List<HierarchicalCategoryInfo>();

            // Create category objects with proper hierarchy
            foreach (var (name, url, slug, productCount) in categories)
            {
                var level = DetermineLevel(slug);
                var parentSlug = FindParentSlug(slug);

                var category = new HierarchicalCategoryInfo(name, slug, url, level, parentSlug);
                result.Add(category);
            }

            return result;
        }

        /// <summary>
        /// Builds hierarchical structure with dynamic parent detection based on URL patterns.
        /// This method can handle unlimited nesting levels by analyzing URL patterns.
        /// </summary>
        /// <param name="categories">Flat list of categories</param>
        /// <param name="parentUrl">URL of the parent category being scanned</param>
        /// <returns>Categories with proper hierarchy relationships</returns>
        public List<HierarchicalCategoryInfo> BuildHierarchyWithParentContext(
            List<(string name, string url, string slug, int productCount)> categories,
            string? parentUrl = null)
        {
            var result = new List<HierarchicalCategoryInfo>();

            // Extract parent slug from URL if provided
            string? parentSlug = null;
            if (!string.IsNullOrEmpty(parentUrl))
            {
                parentSlug = ExtractSlugFromUrl(parentUrl);
            }

            // Create category objects with dynamic hierarchy
            foreach (var (name, url, slug, productCount) in categories)
            {
                var level = DetermineLevelDynamically(slug, parentSlug);
                var detectedParentSlug = FindParentSlugDynamically(slug, parentSlug);

                var category = new HierarchicalCategoryInfo(name, slug, url, level, detectedParentSlug);
                result.Add(category);
            }

            return result;
        }

        /// <summary>
        /// Determines the hierarchical level of a category based on known relationships.
        /// </summary>
        /// <param name="slug">Category slug</param>
        /// <returns>0 for top-level categories, 1+ for subcategories</returns>
        private static int DetermineLevel(string slug)
        {
            return KaspiCategoryHierarchyConfig.KnownParentRelationships.ContainsKey(slug) ? 1 : 0;
        }

        /// <summary>
        /// Finds the parent slug for a given category slug.
        /// </summary>
        /// <param name="slug">Category slug</param>
        /// <returns>Parent slug if found, null otherwise</returns>
        private static string? FindParentSlug(string slug)
        {
            return KaspiCategoryHierarchyConfig.KnownParentRelationships.TryGetValue(slug, out var parent) ? parent : null;
        }

        /// <summary>
        /// Dynamically determines category level based on URL depth and parent context.
        /// </summary>
        private static int DetermineLevelDynamically(string slug, string? parentSlug)
        {
            // If we have a parent context, this is at least level 1
            if (!string.IsNullOrEmpty(parentSlug))
            {
                // Count URL segments to estimate depth
                var segments = slug.Split(new[] { "%20", "_", "-" }, StringSplitOptions.RemoveEmptyEntries);
                var parentSegments = parentSlug.Split(new[] { "%20", "_", "-" }, StringSplitOptions.RemoveEmptyEntries);

                return Math.Max(1, segments.Length - parentSegments.Length + 1);
            }

            // Fallback to static configuration
            return KaspiCategoryHierarchyConfig.KnownParentRelationships.ContainsKey(slug) ? 1 : 0;
        }

        /// <summary>
        /// Dynamically finds parent slug based on context and URL patterns.
        /// </summary>
        private static string? FindParentSlugDynamically(string slug, string? contextParentSlug)
        {
            // If we have context from the scanning process, use it
            if (!string.IsNullOrEmpty(contextParentSlug))
            {
                return contextParentSlug;
            }

            // Fallback to static configuration
            return KaspiCategoryHierarchyConfig.KnownParentRelationships.TryGetValue(slug, out var parent) ? parent : null;
        }

        /// <summary>
        /// Extracts slug from category URL.
        /// </summary>
        private static string ExtractSlugFromUrl(string url)
        {
            // Extract the category slug from URL like https://kaspi.kz/shop/c/women%20fashion/
            var uri = new Uri(url);
            var segments = uri.Segments;
            var categorySegment = segments.LastOrDefault(s => !string.IsNullOrEmpty(s) && s != "/");
            return categorySegment?.TrimEnd('/') ?? string.Empty;
        }
    }
}
