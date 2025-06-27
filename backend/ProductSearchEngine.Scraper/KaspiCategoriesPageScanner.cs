using HtmlAgilityPack;
using ProductSearchEngine.Scraper.AntiDetection;
using ProductSearchEngine.Scraper.Models;
using ProductSearchEngine.Scraper.Services;

namespace ProductSearchEngine.Scraper
{
    public class KaspiCategoriesPageScanner
    {
        private readonly HttpClient _httpClient;
        private readonly CategoryValidationService _validationService;
        private readonly CategoryHierarchyBuilder _hierarchyBuilder;
        private readonly CategoryHierarchyMatcher _hierarchyMatcher;

        public KaspiCategoriesPageScanner(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("KaspiScraper");
            _validationService = new CategoryValidationService();
            _hierarchyBuilder = new CategoryHierarchyBuilder();
            _hierarchyMatcher = new CategoryHierarchyMatcher();
            SetupAntiDetectionHeaders(); // Add anti-detection headers for API usage too
        }

        public KaspiCategoriesPageScanner()
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

            Console.WriteLine("🔒 Anti-detection headers configured for categories page scraping");
        }

        public async Task<List<HierarchicalCategoryInfo>> ScanCategoriesPageAsync(string categoriesUrl = "https://kaspi.kz/shop/c/categories/")
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

                Console.WriteLine($"🌐 Fetching categories page: {categoriesUrl}");

                var response = await _httpClient.GetAsync(categoriesUrl);

                // Handle different HTTP status codes gracefully
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️  HTTP {(int)response.StatusCode} {response.StatusCode} received from {categoriesUrl}");

                    if (isCIEnvironment)
                    {
                        Console.WriteLine("🤖 CI Environment Note: This failure is often expected");
                        Console.WriteLine("   - Production sites frequently block CI/CD runners");
                        Console.WriteLine("   - Geographic restrictions may apply");
                        Console.WriteLine("   - IP-based blocking is common");
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("📄 Categories page not found (404). This might indicate:");
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

                Console.WriteLine("🔍 Parsing HTML for hierarchical category structure...");

                // Look for category containers with hierarchical structure
                var categoryContainers = document.DocumentNode.SelectNodes("//div[contains(@class, 'category-tree')] | //div[contains(@class, 'categories-tree')] | //div[contains(@class, 'shop-categories')] | //ul[contains(@class, 'categories')] | //div[contains(@class, 'categories-list')]");

                if (categoryContainers == null)
                {
                    Console.WriteLine("🔍 No structured category containers found, trying generic link extraction...");
                    return await ExtractFlatCategoryStructure(document);
                }

                Console.WriteLine($"📊 Found {categoryContainers.Count} potential category container(s)");

                foreach (var container in categoryContainers)
                {
                    ExtractHierarchicalStructure(container, allCategories, categoryMap);
                }

                // If no hierarchical structure found, fall back to flat extraction
                if (allCategories.Count == 0)
                {
                    Console.WriteLine("🔄 No hierarchical structure found, falling back to flat extraction...");
                    return await ExtractFlatCategoryStructure(document);
                }

                // Establish parent-child relationships
                _hierarchyMatcher.EstablishRelationships(allCategories);

                Console.WriteLine($"📊 Total categories found: {allCategories.Count}");
                Console.WriteLine($"📊 Top-level categories: {allCategories.Count(c => c.Level == 0)}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error scanning categories page: {ex.Message}");
            }

            return allCategories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
        }

        private Task<List<HierarchicalCategoryInfo>> ExtractFlatCategoryStructure(HtmlDocument document)
        {
            var allCategories = new List<HierarchicalCategoryInfo>();
            var categoryMap = new Dictionary<string, HierarchicalCategoryInfo>();

            Console.WriteLine("🔍 Extracting flat category structure with improved hierarchy detection...");

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

            // Look for various possible category link patterns using proper XPath syntax
            var linkSelectors = new[]
            {
                "//a[contains(@href, '/shop/c/') and not(contains(@href, '/categories/'))]",
                "//a[contains(@href, '/c/') and not(contains(@href, '/categories/'))]"
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
                            foundLinks.Add(href);

                            var fullUrl = href.StartsWith("http") ? href : $"https://kaspi.kz{href}";
                            var slug = _validationService.ExtractSlugFromUrl(fullUrl);
                            var productCount = ExtractProductCountFromLink(link);

                            if (!string.IsNullOrEmpty(slug))
                            {
                                processedCategories.Add((text, fullUrl, slug, productCount));
                            }
                        }
                    }
                }
            }

            // Now process categories and determine hierarchy based on URL patterns and names
            allCategories = _hierarchyBuilder.BuildHierarchy(processedCategories);

            // Establish parent-child relationships
            _hierarchyMatcher.EstablishRelationships(allCategories);

            Console.WriteLine($"✅ Processed {allCategories.Count} categories with hierarchy");
            Console.WriteLine($"📊 Top-level categories: {allCategories.Count(c => c.Level == 0)}");
            Console.WriteLine($"📊 Categories with subcategories: {allCategories.Count(c => c.Subcategories.Any())}");

            return Task.FromResult(allCategories);
        }

        private void ExtractHierarchicalStructure(HtmlNode container, List<HierarchicalCategoryInfo> allCategories, Dictionary<string, HierarchicalCategoryInfo> categoryMap)
        {
            // Implementation for structured category containers
            // This would parse properly structured HTML if found
            Console.WriteLine("🔍 Attempting to extract from structured category container...");
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
    }
}
