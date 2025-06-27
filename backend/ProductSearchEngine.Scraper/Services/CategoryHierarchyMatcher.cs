using System.Collections.Generic;
using System.Linq;
using ProductSearchEngine.Scraper.Models;

namespace ProductSearchEngine.Scraper.Services
{
    /// <summary>
    /// Service responsible for establishing parent-child relationships in category hierarchy.
    /// </summary>
    public class CategoryHierarchyMatcher
    {
        /// <summary>
        /// Establishes parent-child relationships by populating Subcategories collections.
        /// </summary>
        /// <param name="categories">List of categories with parent-child relationships defined</param>
        public void EstablishRelationships(List<HierarchicalCategoryInfo> categories)
        {
            var categoryMap = categories.ToDictionary(c => c.Slug, c => c);

            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.ParentSlug) &&
                    categoryMap.TryGetValue(category.ParentSlug, out var parent))
                {
                    parent.Subcategories.Add(category);
                }
            }
        }
    }
}
