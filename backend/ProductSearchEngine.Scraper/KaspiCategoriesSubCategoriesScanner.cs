using HtmlAgilityPack;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Services;

namespace ProductSearchEngine.Scraper
{
    /// <summary>
    /// Scanner for extracting subcategories from specific Kaspi category pages.
    /// This scanner takes a category URL (e.g., https://kaspi.kz/shop/c/smartphones%20and%20gadgets/)
    /// and extracts all subcategories and sub-subcategories found on that page.
    /// </summary>
    public class KaspiCategoriesSubCategoriesScanner
    {
        private readonly HttpClient _httpClient;
        private readonly CategoryValidationService _validationService;
        private readonly CategoryHierarchyBuilder _hierarchyBuilder;
        private readonly CategoryHierarchyMatcher _hierarchyMatcher;

        public KaspiCategoriesSubCategoriesScanner(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("KaspiScraper");
            _validationService = new CategoryValidationService();
            _hierarchyBuilder = new CategoryHierarchyBuilder();
            _hierarchyMatcher = new CategoryHierarchyMatcher();
            SetupAntiDetectionHeaders();
        }

        public KaspiCategoriesSubCategoriesScanner()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _validationService = new CategoryValidationService();
            _hierarchyBuilder = new CategoryHierarchyBuilder();
            _hierarchyMatcher = new CategoryHierarchyMatcher();
            SetupAntiDetectionHeaders();
        }

        private void SetupAntiDetectionHeaders()
        {
            var headerGenerator = new HeaderGenerator();
            var strategy = new AntiDetectionStrategy
            {
                RotateUserAgent = true,
                RandomHeaders = true,
                SimulateBrowserFingerprint = true,
                AddReferrerPath = true
            };

            var headers = headerGenerator.GenerateHeaders(strategy);

            // Clear any existing headers
            _httpClient.DefaultRequestHeaders.Clear();

            // Add realistic browser headers
            foreach (var header in headers)
            {
                try
                {
                    if (header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    else if (header.Key.Equals("Accept-Language", StringComparison.OrdinalIgnoreCase))
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    // Skip Accept-Encoding to let automatic decompression handle it
                    else if (!header.Key.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase) && !header.Key.StartsWith("Content-"))
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Could not add header {header.Key}: {ex.Message}");
                }
            }

            Console.WriteLine("🔒 Anti-detection headers configured for subcategories page scraping");
        }

        /// <summary>
        /// Scans a specific category page to extract its subcategories and sub-subcategories.
        /// </summary>
        /// <param name="categoryUrl">URL of the category page to scan (e.g., https://kaspi.kz/shop/c/smartphones%20and%20gadgets/)</param>
        /// <param name="parentCategoryName">Name of the parent category for hierarchy building (optional)</param>
        /// <param name="parentCategorySlug">Slug of the parent category for hierarchy building (optional)</param>
        /// <returns>List of subcategories found on the category page</returns>
        public async Task<List<HierarchicalCategoryInfo>> ScanCategorySubcategoriesAsync(
            string categoryUrl,
            string? parentCategoryName = null,
            string? parentCategorySlug = null)
        {
            var allCategories = new List<HierarchicalCategoryInfo>();
            var categoryMap = new Dictionary<string, HierarchicalCategoryInfo>();

            try
            {
                // Enhanced logging for CI/CD debugging
                var isCIEnvironment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                                     !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

                if (isCIEnvironment)
                {
                    Console.WriteLine("🤖 CI/CD Environment Detected");
                    Console.WriteLine($"   CI: {Environment.GetEnvironmentVariable("CI")}");
                    Console.WriteLine($"   GITHUB_ACTIONS: {Environment.GetEnvironmentVariable("GITHUB_ACTIONS")}");
                    Console.WriteLine($"   User Agent: {_httpClient.DefaultRequestHeaders.UserAgent}");
                    Console.WriteLine("   Note: Production sites often block CI environments");
                }

                Console.WriteLine($"🌐 Fetching category page for subcategories: {categoryUrl}");
                if (!string.IsNullOrEmpty(parentCategoryName))
                {
                    Console.WriteLine($"   Parent category: {parentCategoryName} ({parentCategorySlug})");
                }

                var response = await _httpClient.GetAsync(categoryUrl);

                // Handle different HTTP status codes gracefully
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️  HTTP {(int)response.StatusCode} {response.StatusCode} received from {categoryUrl}");

                    if (isCIEnvironment)
                    {
                        Console.WriteLine("🤖 CI Environment Note: This failure is often expected");
                        Console.WriteLine("   - Production sites frequently block CI/CD runners");
                        Console.WriteLine("   - Geographic restrictions may apply");
                        Console.WriteLine("   - IP-based blocking is common");
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("📄 Category page not found (404). This might indicate:");
                        Console.WriteLine("   - URL structure has changed");
                        Console.WriteLine("   - Page has been moved or removed");
                        Console.WriteLine("   - Anti-bot measures are blocking access");
                        return allCategories; // Return empty list instead of throwing
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Console.WriteLine("🚫 Access forbidden (403). This might indicate:");
                        Console.WriteLine("   - Anti-bot detection triggered");
                        Console.WriteLine("   - IP address blocked");
                        Console.WriteLine("   - User-Agent not accepted");
                        return allCategories; // Return empty list instead of throwing
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine("🛑 Rate limited (429). This indicates:");
                        Console.WriteLine("   - Too many requests sent");
                        Console.WriteLine("   - Need to implement request throttling");
                        return allCategories; // Return empty list instead of throwing
                    }
                    else
                    {
                        Console.WriteLine($"❌ Unexpected HTTP status: {response.StatusCode}");
                        Console.WriteLine($"   Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                        response.EnsureSuccessStatusCode(); // Still throw for other unexpected errors
                    }
                }

                var html = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Retrieved HTML content ({html.Length} characters)");

                if (string.IsNullOrWhiteSpace(html) || html.Length < 100)
                {
                    Console.WriteLine("⚠️  Received empty or very short HTML response");
                    Console.WriteLine("   This might indicate a captcha page or blocked request");
                    return allCategories;
                }

                // Debug: Save a small sample of HTML to examine structure
                var htmlSample = html.Substring(0, Math.Min(html.Length, 2000));
                Console.WriteLine($"🔍 HTML Sample (first 2000 chars): {htmlSample}");

                var document = new HtmlDocument();
                document.LoadHtml(html);

                Console.WriteLine("🔍 Parsing HTML for subcategory structure...");

                // Extract parent category slug from URL if not provided
                if (string.IsNullOrEmpty(parentCategorySlug))
                {
                    parentCategorySlug = _validationService.ExtractSlugFromUrl(categoryUrl);
                }

                // Look for subcategory containers with hierarchical structure (similar patterns as main categories)
                var categoryContainers = document.DocumentNode.SelectNodes(
                    "//div[contains(@class, 'category-tree')] | " +
                    "//div[contains(@class, 'categories-tree')] | " +
                    "//div[contains(@class, 'shop-categories')] | " +
                    "//ul[contains(@class, 'categories')] | " +
                    "//div[contains(@class, 'categories-list')] | " +
                    "//div[contains(@class, 'subcategories')] | " +
                    "//nav[contains(@class, 'category-nav')] | " +
                    "//div[contains(@class, 'filter-categories')]");

                if (categoryContainers == null)
                {
                    Console.WriteLine("🔍 No structured subcategory containers found, trying generic link extraction...");
                    return await ExtractFlatSubcategoryStructure(document, parentCategorySlug, categoryUrl);
                }

                Console.WriteLine($"📊 Found {categoryContainers.Count} potential subcategory container(s)");

                foreach (var container in categoryContainers)
                {
                    ExtractHierarchicalSubcategoryStructure(container, allCategories, categoryMap, parentCategorySlug);
                }

                // If no hierarchical structure found, fall back to flat extraction
                if (allCategories.Count == 0)
                {
                    Console.WriteLine("🔄 No hierarchical subcategory structure found, falling back to flat extraction...");
                    return await ExtractFlatSubcategoryStructure(document, parentCategorySlug, categoryUrl);
                }

                // Establish parent-child relationships
                _hierarchyMatcher.EstablishRelationships(allCategories);

                Console.WriteLine($"📊 Total subcategories found: {allCategories.Count}");
                Console.WriteLine($"📊 Level 1 subcategories: {allCategories.Count(c => c.Level == 1)}");
                Console.WriteLine($"📊 Level 2+ subcategories: {allCategories.Count(c => c.Level > 1)}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error scanning category page for subcategories: {ex.Message}");
            }

            return allCategories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
        }

        private Task<List<HierarchicalCategoryInfo>> ExtractFlatSubcategoryStructure(HtmlDocument document, string? parentCategorySlug, string categoryUrl)
        {
            var allCategories = new List<HierarchicalCategoryInfo>();

            Console.WriteLine("🔍 Extracting flat subcategory structure...");

            // First, let's debug what links are actually present
            var allLinks = document.DocumentNode.SelectNodes("//a[@href]");
            Console.WriteLine($"🔍 Total links found on page: {allLinks?.Count ?? 0}");

            if (allLinks != null)
            {
                var categoryLinks = allLinks.Where(l => l.GetAttributeValue("href", "").Contains("/c/")).Take(10).ToList();
                Console.WriteLine($"🔍 Sample links containing '/c/': {categoryLinks.Count}");
                foreach (var link in categoryLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    var text = link.InnerText?.Trim();
                    Console.WriteLine($"   Link: {href} -> {text}");
                }
            }

            // Look for various possible subcategory link patterns
            var linkSelectors = new[]
            {
                // Standard category links but excluding the current page
                "//a[contains(@href, '/shop/c/') and not(contains(@href, '/categories/'))]",
                "//a[contains(@href, '/c/') and not(contains(@href, '/categories/'))]",
                // Sidebar or navigation category links
                "//nav//a[contains(@href, '/c/')]",
                "//div[contains(@class, 'sidebar')]//a[contains(@href, '/c/')]",
                "//div[contains(@class, 'category-filter')]//a[contains(@href, '/c/')]",
                "//div[contains(@class, 'filter')]//a[contains(@href, '/c/')]",
                // Breadcrumb-adjacent links (subcategories might be listed near breadcrumbs)
                "//div[contains(@class, 'breadcrumb')]//following-sibling::*//a[contains(@href, '/c/')]",
                // Category cards or tiles
                "//div[contains(@class, 'category-card')]//a[contains(@href, '/c/')]",
                "//div[contains(@class, 'category-tile')]//a[contains(@href, '/c/')]"
            };

            var foundLinks = new HashSet<string>();
            var processedCategories = new List<(string name, string url, string slug, int productCount)>();

            foreach (var selector in linkSelectors)
            {
                var links = document.DocumentNode.SelectNodes(selector);
                Console.WriteLine($"🔍 Selector '{selector}' found {links?.Count ?? 0} links");
                if (links != null)
                {
                    Console.WriteLine($"  Found {links.Count} links with selector: {selector}");

                    foreach (var link in links)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        var text = link.InnerText?.Trim() ?? string.Empty;

                        if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text) &&
                            _validationService.IsValidCategoryLink(href, text) && !foundLinks.Contains(href))
                        {
                            var fullUrl = href.StartsWith("http") ? href : $"https://kaspi.kz{href}";
                            var slug = _validationService.ExtractSlugFromUrl(fullUrl);

                            // Skip if this is the same as the parent category we're scanning
                            if (!string.IsNullOrEmpty(slug) && slug != parentCategorySlug)
                            {
                                foundLinks.Add(href);
                                var productCount = ExtractProductCountFromLink(link);
                                processedCategories.Add((text, fullUrl, slug, productCount));
                            }
                        }
                    }
                }
            }

            // Build hierarchy with parent context for better nesting support
            allCategories = _hierarchyBuilder.BuildHierarchyWithParentContext(processedCategories, categoryUrl);

            // For subcategory scanning, we need to detect proper levels based on semantic relationships
            if (!string.IsNullOrEmpty(parentCategorySlug))
            {
                // Create a new list with corrected parent relationships
                var adjustedCategories = new List<HierarchicalCategoryInfo>();

                // First pass: identify main subcategories (Level 1)
                var mainSubcategories = IdentifyMainSubcategories(allCategories);

                foreach (var category in allCategories)
                {
                    var (level, detectedParent) = DetermineHierarchicalLevel(category, mainSubcategories, parentCategorySlug);

                    var adjustedCategory = category with
                    {
                        Level = level,
                        ParentSlug = detectedParent
                    };
                    adjustedCategories.Add(adjustedCategory);
                }

                allCategories = adjustedCategories;
            }

            // Establish parent-child relationships
            _hierarchyMatcher.EstablishRelationships(allCategories);

            Console.WriteLine($"✅ Processed {allCategories.Count} subcategories");
            Console.WriteLine($"📊 Level 1 subcategories: {allCategories.Count(c => c.Level == 1)}");
            Console.WriteLine($"📊 Level 2+ subcategories: {allCategories.Count(c => c.Level > 1)}");

            return Task.FromResult(allCategories);
        }

        private void ExtractHierarchicalSubcategoryStructure(HtmlNode container, List<HierarchicalCategoryInfo> allCategories, Dictionary<string, HierarchicalCategoryInfo> categoryMap, string? parentCategorySlug)
        {
            // Implementation for structured subcategory containers
            // This would parse properly structured HTML if found
            Console.WriteLine("🔍 Attempting to extract from structured subcategory container...");

            // Look for nested list structures or category cards within the container
            var nestedLinks = container.SelectNodes(".//a[contains(@href, '/c/')]");
            if (nestedLinks != null)
            {
                Console.WriteLine($"  Found {nestedLinks.Count} nested category links in structured container");

                foreach (var link in nestedLinks)
                {
                    var href = link.GetAttributeValue("href", string.Empty);
                    var text = link.InnerText?.Trim() ?? string.Empty;

                    if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text) &&
                        _validationService.IsValidCategoryLink(href, text))
                    {
                        var fullUrl = href.StartsWith("http") ? href : $"https://kaspi.kz{href}";
                        var slug = _validationService.ExtractSlugFromUrl(fullUrl);

                        if (!string.IsNullOrEmpty(slug) && slug != parentCategorySlug)
                        {
                            var level = string.IsNullOrEmpty(parentCategorySlug) ? 0 : 1;

                            var category = new HierarchicalCategoryInfo(text, slug, fullUrl, level, parentCategorySlug);

                            if (!categoryMap.ContainsKey(slug))
                            {
                                allCategories.Add(category);
                                categoryMap[slug] = category;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a category slug represents a sub-subcategory based on relationship analysis.
        /// </summary>
        private bool IsSubSubcategory(string slug, string? parentSlug, List<(string name, string url, string slug, int productCount)> allCategories)
        {
            // Simple heuristic: if there's another category that could be the intermediate parent
            // This is a simplified approach - could be enhanced with more sophisticated logic

            if (string.IsNullOrEmpty(parentSlug))
                return false;

            // Look for potential intermediate parents
            var decodedSlug = Uri.UnescapeDataString(slug);
            var decodedParent = Uri.UnescapeDataString(parentSlug);

            foreach (var (name, url, categorySlug, productCount) in allCategories)
            {
                if (categorySlug != slug && categorySlug != parentSlug)
                {
                    var decodedCategory = Uri.UnescapeDataString(categorySlug);

                    // If this category's name/slug appears to be between parent and current slug in hierarchy
                    if (decodedSlug.Contains(decodedCategory) && decodedCategory.Contains(decodedParent))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int ExtractProductCountFromLink(HtmlNode link)
        {
            // Look for product counts in various places
            var countText = string.Empty;

            // Check for count in text content
            var fullText = link.InnerText ?? string.Empty;
            var countMatch = System.Text.RegularExpressions.Regex.Match(fullText, @"\((\d+(?:\s?\d+)*)\)");
            if (countMatch.Success)
            {
                countText = countMatch.Groups[1].Value.Replace(" ", "");
                if (int.TryParse(countText, out int count))
                {
                    return count;
                }
            }

            // Check for count in adjacent elements
            var nextSibling = link.NextSibling;
            while (nextSibling != null)
            {
                if (nextSibling.NodeType == HtmlNodeType.Text || nextSibling.NodeType == HtmlNodeType.Element)
                {
                    var siblingText = nextSibling.InnerText ?? string.Empty;
                    countMatch = System.Text.RegularExpressions.Regex.Match(siblingText, @"\((\d+(?:\s?\d+)*)\)");
                    if (countMatch.Success)
                    {
                        countText = countMatch.Groups[1].Value.Replace(" ", "");
                        if (int.TryParse(countText, out int count))
                        {
                            return count;
                        }
                    }
                }
                nextSibling = nextSibling.NextSibling;
            }

            return 0;
        }

        /// <summary>
        /// Recursively scans a category and all its subcategories to build a complete hierarchy tree.
        /// This method can handle unlimited nesting depth (e.g., category -> subcategory -> subsubcategory -> etc.)
        /// </summary>
        /// <param name="categoryUrl">URL of the category page to scan</param>
        /// <param name="parentCategoryName">Name of the parent category</param>
        /// <param name="parentCategorySlug">Slug of the parent category</param>
        /// <param name="maxDepth">Maximum depth to scan (prevents infinite recursion)</param>
        /// <param name="currentDepth">Current scanning depth</param>
        /// <returns>Complete hierarchy tree including all nested subcategories</returns>
        public async Task<List<HierarchicalCategoryInfo>> ScanCategoryHierarchyRecursivelyAsync(
            string categoryUrl,
            string? parentCategoryName = null,
            string? parentCategorySlug = null,
            int maxDepth = 5,
            int currentDepth = 0)
        {
            if (currentDepth >= maxDepth)
            {
                Console.WriteLine($"🛑 Max depth {maxDepth} reached, stopping recursive scan");
                return new List<HierarchicalCategoryInfo>();
            }

            Console.WriteLine($"🔍 Recursive scan at depth {currentDepth}: {categoryUrl}");

            // Scan current level
            var currentLevelCategories = await ScanCategorySubcategoriesAsync(categoryUrl, parentCategoryName, parentCategorySlug);

            var allCategories = new List<HierarchicalCategoryInfo>(currentLevelCategories);

            // Recursively scan each subcategory found
            foreach (var subcategory in currentLevelCategories)
            {
                try
                {
                    Console.WriteLine($"🔄 Recursively scanning subcategory: {subcategory.Name} ({subcategory.Url})");

                    var deeperCategories = await ScanCategoryHierarchyRecursivelyAsync(
                        subcategory.Url,
                        subcategory.Name,
                        subcategory.Slug,
                        maxDepth,
                        currentDepth + 1);

                    // Add deeper categories and establish parent relationships
                    foreach (var deepCategory in deeperCategories)
                    {
                        // Ensure proper parent relationship
                        var adjustedCategory = deepCategory with
                        {
                            Level = currentDepth + 2, // Adjust level based on current depth
                            ParentSlug = subcategory.Slug // Set correct parent
                        };
                        allCategories.Add(adjustedCategory);

                        // Add to subcategory's children
                        subcategory.Subcategories.Add(adjustedCategory);
                    }

                    // Small delay to be respectful to the server
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error scanning subcategory {subcategory.Name}: {ex.Message}");
                    // Continue with other subcategories
                }
            }

            Console.WriteLine($"✅ Recursive scan completed at depth {currentDepth}: found {allCategories.Count} total categories");
            return allCategories;
        }

        /// <summary>
        /// Identifies main subcategories (Level 1) based on semantic analysis
        /// </summary>
        private static List<HierarchicalCategoryInfo> IdentifyMainSubcategories(List<HierarchicalCategoryInfo> allCategories)
        {
            var mainSubcategories = new List<HierarchicalCategoryInfo>();

            // Main category keywords that indicate top-level subcategories
            var mainCategoryKeywords = new[]
            {
                "смартфон", "smartphone", "phone", "телефон", "мобильн",
                "смарт-час", "smart watch", "watch", "час",
                "аксессуар", "accessory", "accessories",
                "гаджет", "gadget",
                "рац", "radio", "walkie", "cb radio",
                "фитнес", "fitness", "wearable",
                "электронн", "electronic", "ebook"
            };

            foreach (var category in allCategories)
            {
                var nameWords = category.Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var isMainCategory = false;

                // Check if this is a main category based on keywords
                foreach (var keyword in mainCategoryKeywords)
                {
                    if (nameWords.Any(word => word.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Check if it's not too specific (not a sub-subcategory)
                        var specificityScore = CalculateSpecificityScore(category.Name);
                        if (specificityScore <= 2) // Not too specific
                        {
                            isMainCategory = true;
                            break;
                        }
                    }
                }

                if (isMainCategory)
                {
                    mainSubcategories.Add(category);
                }
            }

            return mainSubcategories;
        }

        /// <summary>
        /// Determines the hierarchical level and parent for a category
        /// </summary>
        private static (int level, string parentSlug) DetermineHierarchicalLevel(
            HierarchicalCategoryInfo category,
            List<HierarchicalCategoryInfo> mainSubcategories,
            string fallbackParentSlug)
        {
            // Check if this is a main subcategory
            if (mainSubcategories.Any(m => m.Slug == category.Slug))
            {
                return (1, fallbackParentSlug);
            }

            // Try to find a semantic parent among main subcategories
            var semanticParent = FindSemanticParent(category, mainSubcategories);
            if (semanticParent != null)
            {
                return (2, semanticParent.Slug);
            }

            // Check specificity to determine level
            var specificityScore = CalculateSpecificityScore(category.Name);
            var level = Math.Min(specificityScore, 3); // Cap at level 3

            return (level, fallbackParentSlug);
        }

        /// <summary>
        /// Finds a semantic parent category based on name similarity
        /// </summary>
        private static HierarchicalCategoryInfo? FindSemanticParent(
            HierarchicalCategoryInfo category,
            List<HierarchicalCategoryInfo> mainSubcategories)
        {
            var categoryWords = category.Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var bestMatch = mainSubcategories
                .Select(main => new
                {
                    Category = main,
                    Score = CalculateSemanticSimilarity(categoryWords, main.Name.ToLowerInvariant())
                })
                .Where(x => x.Score > 0.3) // Minimum similarity threshold
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch?.Category;
        }

        /// <summary>
        /// Calculates specificity score based on number of descriptive words
        /// </summary>
        private static int CalculateSpecificityScore(string categoryName)
        {
            var words = categoryName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // More words usually means more specific subcategory
            if (words.Length >= 6) return 3; // Very specific (Level 3)
            if (words.Length >= 4) return 2; // Moderately specific (Level 2) 
            return 1; // General (Level 1)
        }

        /// <summary>
        /// Calculates semantic similarity between category name and main category
        /// </summary>
        private static double CalculateSemanticSimilarity(string[] categoryWords, string mainCategoryName)
        {
            var mainWords = mainCategoryName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var sharedWords = categoryWords.Intersect(mainWords, StringComparer.OrdinalIgnoreCase).Count();
            var totalWords = categoryWords.Union(mainWords, StringComparer.OrdinalIgnoreCase).Count();

            if (totalWords == 0) return 0;

            return (double)sharedWords / totalWords;
        }
    }
}
