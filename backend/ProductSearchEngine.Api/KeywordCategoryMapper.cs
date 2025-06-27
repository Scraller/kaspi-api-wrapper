using System.Collections.Generic;
using System.Linq;

namespace ProductSearchEngine.Api
{
    /// <summary>
    /// Maps search keywords to appropriate product categories for Kaspi.kz
    /// </summary>
    public static class KeywordCategoryMapper
    {
        // Simple keyword-to-category mapping for Kaspi
        private static readonly Dictionary<string, string> KeywordToCategory = new()
        {
            { "phone", "smartphones" },
            { "smartphones", "smartphones" },
            { "смартфон", "smartphones" },
            { "телефон", "smartphones" },
            { "laptop", "notebooks" },
            { "laptops", "notebooks" },
            { "ноутбук", "notebooks" },
            { "ноутбуки", "notebooks" },
            { "tv", "televisions" },
            { "tvs", "televisions" },
            { "телевизор", "televisions" },
            { "телевизоры", "televisions" },
            // Add more mappings as needed
        };

        /// <summary>
        /// Maps a search query to an appropriate product category
        /// </summary>
        /// <param name="query">The search query to map</param>
        /// <returns>The corresponding category name, or null if no mapping is found</returns>
        public static string? Map(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var lower = query.ToLowerInvariant().Trim();
            Console.WriteLine($"[CATEGORY][DEBUG] Mapping category for query: '{query}'");

            // First try exact match
            if (KeywordToCategory.TryGetValue(lower, out var exactCategory))
            {
                Console.WriteLine($"[CATEGORY][DEBUG] Found exact match: '{lower}' -> '{exactCategory}'");
                return exactCategory;
            }

            // Try singularize/pluralize variants (simple English rules)
            var singularized = lower.EndsWith("s") ? lower.Substring(0, lower.Length - 1) : lower;
            var pluralized = lower.EndsWith("s") ? lower : lower + "s";

            if (KeywordToCategory.TryGetValue(singularized, out var singularCategory))
            {
                Console.WriteLine($"[CATEGORY][DEBUG] Found singular match: '{singularized}' -> '{singularCategory}'");
                return singularCategory;
            }

            if (KeywordToCategory.TryGetValue(pluralized, out var pluralCategory))
            {
                Console.WriteLine($"[CATEGORY][DEBUG] Found plural match: '{pluralized}' -> '{pluralCategory}'");
                return pluralCategory;
            }

            // Try contains as last resort
            foreach (var kvp in KeywordToCategory)
            {
                if (lower.Contains(kvp.Key))
                {
                    Console.WriteLine($"[CATEGORY][DEBUG] Found partial match: '{kvp.Key}' in '{lower}' -> '{kvp.Value}'");
                    return kvp.Value;
                }

                // Also check if any of our keywords is contained in the query
                if (kvp.Key.Contains(lower) && lower.Length > 2)
                {
                    Console.WriteLine($"[CATEGORY][DEBUG] Found keyword match: '{lower}' in '{kvp.Key}' -> '{kvp.Value}'");
                    return kvp.Value;
                }
            }

            Console.WriteLine($"[CATEGORY][DEBUG] No category match found for '{query}'");
            return null;
        }
    }
}
