#nullable disable
using System.Collections.Generic;
using NUnit.Framework;

namespace ProductSearchEngine.Tests.TestData;

/// <summary>
/// Comprehensive test data matrix for API endpoint testing with edge cases and validation scenarios
/// </summary>
public static class ApiTestDataMatrix
{
    #region Product Search Test Data
    
    /// <summary>
    /// Test data for GET /api/Products/search endpoint
    /// Required: text (string)
    /// Optional: category (string), cityCode (string), page (int), pageSize (int)
    /// </summary>
    public static readonly object[][] ProductSearchTestCases = 
    [
        // Happy path cases
        ["iPhone", null, "750000000", 0, 20, true, "Basic search with popular product - first page"],
        ["Samsung Galaxy", "phones", "750000000", 0, 10, true, "Search with category filter - first page"],
        ["laptop", null, "710000000", 1, 50, true, "Search in different city (Nur-Sultan) - second page"],
        ["телефон", null, "750000000", 0, 5, true, "Search in Kazakh/Russian - first page"],
        
        // Edge cases - Valid
        ["ab", null, "750000000", 0, 1, true, "Minimum valid search length - first page"],
        ["supercalifragilisticexpialidocious smartphone case", null, "750000000", 0, 20, true, "Very long search term - first page"],
        ["iPhone 15", null, "750000000", 0, 100, true, "First page, maximum page size (note: Kaspi returns ~12 regardless)"],
        ["MacBook", null, "750000000", 5, 20, true, "High page number"],
        
        // Edge cases - Invalid (should return 400)
        ["", null, "750000000", 0, 20, false, "Empty search text"],
        ["a", null, "750000000", 0, 20, false, "Search text too short"],
        ["iPhone", null, "750000000", -1, 20, false, "Negative page number"],
        ["iPhone", null, "750000000", 0, 0, false, "Zero page size"],
        ["iPhone", null, "750000000", 0, 101, false, "Page size exceeds maximum"],
        ["iPhone", null, "750000000", 0, -5, false, "Negative page size"],
        
        // Special characters and encoding
        ["iPhone 14 Pro Max 256GB", null, "750000000", 0, 20, true, "Search with spaces and numbers"],
        ["кофе-машина", null, "750000000", 0, 20, true, "Search with Cyrillic and hyphen"],
        ["3M™ маска", null, "750000000", 0, 20, true, "Search with trademark symbol"],
        ["iPad+pencil", null, "750000000", 0, 20, true, "Search with plus sign"],
        
        // City code variations
        ["iPhone", null, "710000000", 0, 20, true, "Nur-Sultan city code"],
        ["iPhone", null, "160000000", 0, 20, true, "Shymkent city code"],
        ["iPhone", null, "invalid", 0, 20, true, "Invalid city code (should default)"],
        ["iPhone", null, null, 0, 20, true, "Null city code (should default)"],
        
        // Category variations
        ["gaming", "computers", "750000000", 0, 20, true, "Valid category"],
        ["headphones", "electronics", "750000000", 0, 20, true, "Electronics category"],
        ["dress", "clothing", "750000000", 0, 20, true, "Clothing category"],
        ["book", "invalid-category", "750000000", 0, 20, true, "Invalid category (should ignore)"],
    ];

    #endregion

    #region Product Details Test Data
    
    /// <summary>
    /// Test data for GET /api/Products/{productId} endpoint
    /// Required: productId (string)
    /// Optional: cityCode (string)
    /// </summary>
    public static readonly object[][] ProductDetailsTestCases = 
    [
        // Valid product IDs (commonly known products)
        ["102298404", "750000000", true, "Valid iPhone product ID in Almaty"],
        ["102298404", "710000000", true, "Valid product ID in Nur-Sultan"],
        ["102298404", null, true, "Valid product ID with default city"],
        ["102298404", "", true, "Valid product ID with empty city code"],
        
        // Invalid product IDs
        ["", "750000000", false, "Empty product ID"],
        ["invalid-id", "750000000", false, "Non-numeric product ID"],
        ["999999999", "750000000", false, "Non-existent product ID"],
        ["0", "750000000", false, "Zero product ID"],
        ["-123", "750000000", false, "Negative product ID"],
        
        // Edge cases
        ["123456789012345", "750000000", false, "Very long product ID"],
        ["12345", "invalid-city", true, "Invalid city code"],
        ["  102298404  ", "750000000", true, "Product ID with whitespace"],
    ];

    #endregion

    #region Regions/Cities Test Data
    
    /// <summary>
    /// Test data for GET /api/Regions/cities endpoint
    /// Optional: majorCitiesOnly (bool)
    /// </summary>
    public static readonly object[][] CitiesTestCases = 
    [
        [null, true, "Default - all cities"],
        [true, true, "Major cities only"],
        [false, true, "All cities explicitly"],
    ];

    /// <summary>
    /// Test data for GET /api/Regions/cities/search endpoint
    /// Optional: q (string), majorOnly (bool), limit (int)
    /// </summary>
    public static readonly object[][] CitySearchTestCases = 
    [
        // Valid searches
        ["Алматы", false, 20, true, "Search for Almaty in Cyrillic"],
        ["Almaty", false, 20, true, "Search for Almaty in Latin"],
        ["Нур", true, 10, true, "Partial search for Nur-Sultan, major only"],
        ["", false, 50, true, "Empty search (should return all)"],
        [null, false, 20, true, "Null search query"],
        
        // Edge cases - Valid
        ["А", false, 1, true, "Single character search"],
        ["Алматы", false, 100, true, "Maximum limit"],
        ["city", false, 1, true, "Minimum limit"],
        
        // Edge cases - Invalid
        ["Алматы", false, 0, false, "Zero limit"],
        ["Алматы", false, 101, false, "Limit exceeds maximum"],
        ["Алматы", false, -1, false, "Negative limit"],
    ];

    #endregion

    #region Categories Test Data
    
    /// <summary>
    /// Test data for GET /api/Categories/kaspi/search endpoint
    /// Required: query (string)
    /// Optional: maxResults (int)
    /// </summary>
    public static readonly object[][] CategorySearchTestCases = 
    [
        // Valid searches
        ["телефон", 20, true, "Phone search in Russian"],
        ["computer", 10, true, "Computer search in English"],
        ["одежда", 50, true, "Clothing search in Russian"],
        ["кухня", null, true, "Kitchen search with default limit"],
        
        // Edge cases - Valid
        ["а", 5, true, "Single character search"],
        ["superlongcategoryname", 1, true, "Very long category name"],
        ["категория с пробелами", 20, true, "Category with spaces"],
        
        // Edge cases - Invalid
        ["", 20, false, "Empty search query"],
        ["category", 0, false, "Zero max results"],
        ["category", -1, false, "Negative max results"],
        ["category", 101, false, "Max results exceeds limit"],
    ];

    #endregion

    #region Search Suggestions Test Data
    
    /// <summary>
    /// Test data for GET /api/Search/suggestions endpoint
    /// Required: q (string)
    /// Optional: limit (int), category (string)
    /// </summary>
    public static readonly object[][] SearchSuggestionsTestCases = 
    [
        // Valid searches
        ["iPhone", 10, null, true, "Basic iPhone suggestion"],
        ["Sam", 5, "phones", true, "Partial brand name with category"],
        ["ноут", 15, null, true, "Laptop in Russian (partial)"],
        ["игр", 20, "gaming", true, "Gaming in Russian (partial)"],
        
        // Edge cases - Valid
        ["i", 1, null, true, "Single character"],
        ["smartphone", 50, null, true, "Full word"],
        ["тел", null, null, true, "Partial Cyrillic with default limit"],
        
        // Edge cases - Invalid
        ["", 10, null, false, "Empty query"],
        ["phone", 0, null, false, "Zero limit"],
        ["phone", -1, null, false, "Negative limit"],
        ["phone", 101, null, false, "Limit exceeds maximum"],
    ];

    #endregion

    #region Merchants Test Data
    
    /// <summary>
    /// Test data for merchant-related endpoints
    /// </summary>
    public static readonly object[][] MerchantTestCases = 
    [
        // Valid merchant IDs (if applicable)
        ["valid-merchant-id", true, "Valid merchant lookup"],
        ["", false, "Empty merchant ID"],
        ["invalid-id", false, "Non-existent merchant"],
    ];

    #endregion

    #region Common Test Data Helpers
    
    /// <summary>
    /// Valid Kaspi city codes for testing
    /// </summary>
    public static readonly string[] ValidCityCodes = 
    [
        "750000000", // Almaty
        "710000000", // Nur-Sultan
        "160000000", // Shymkent
        "270000000", // Aktobe
        "470000000", // Karaganda
    ];

    /// <summary>
    /// Invalid city codes for negative testing
    /// </summary>
    public static readonly string[] InvalidCityCodes = 
    [
        "invalid",
        "999999999",
        "0",
        "-1",
        "abc123",
    ];

    /// <summary>
    /// Common search terms in different languages
    /// </summary>
    public static readonly string[] CommonSearchTerms = 
    [
        // English
        "iPhone", "Samsung", "laptop", "headphones", "gaming", "book",
        
        // Russian/Kazakh
        "телефон", "компьютер", "наушники", "игры", "книга", "одежда",
        
        // Mixed
        "iPhone 15", "Samsung Galaxy", "MacBook Pro", "Sony WH-1000XM4",
    ];

    /// <summary>
    /// Edge case strings for testing input validation
    /// </summary>
    public static readonly string[] EdgeCaseStrings = 
    [
        "",                    // Empty
        " ",                   // Whitespace only
        "a",                   // Single character
        "ab",                  // Minimum valid length
        "supercalifragilisticexpialidocious", // Very long
        "  trimme  ",          // Leading/trailing spaces
        "spëcial çhars",       // Special characters
        "数字 123",            // Mixed scripts
        "\"quoted\"",          // Quotes
        "<script>",            // HTML/XSS attempt
        "'; DROP TABLE --",    // SQL injection attempt
    ];

    /// <summary>
    /// Common invalid numeric inputs for testing
    /// </summary>
    public static readonly int[] InvalidNumbers = 
    [
        -1, -100, 0, 101, 1000, int.MaxValue, int.MinValue
    ];

    /// <summary>
    /// Valid pagination test cases
    /// </summary>
    public static readonly object[][] PaginationTestCases = 
    [
        [0, 1, true, "First page, minimum size"],
        [0, 20, true, "First page, default size"],
        [0, 100, true, "First page, maximum size"],
        [1, 20, true, "Second page"],
        [10, 50, true, "High page number"],
        
        // Invalid cases
        [-1, 20, false, "Negative page"],
        [0, 0, false, "Zero page size"],
        [0, 101, false, "Page size too large"],
        [0, -1, false, "Negative page size"],
    ];

    #endregion

    #region Test Case Generators
    
    /// <summary>
    /// Generates test cases for endpoints with query parameters
    /// </summary>
    /// <param name="baseUrl">Base URL pattern</param>
    /// <param name="queryParams">Dictionary of parameter names and test values</param>
    /// <returns>Generated test cases</returns>
    public static IEnumerable<object[]> GenerateQueryParameterTestCases(string baseUrl, Dictionary<string, object[]> queryParams)
    {
        var testCases = new List<object[]>();
        
        // Generate combinations of parameters
        foreach (var param1 in queryParams.First().Value)
        {
            testCases.Add([baseUrl, param1, "Single parameter test"]);
        }
        
        // Add more complex combinations as needed
        return testCases;
    }

    /// <summary>
    /// Generates stress test cases with various input combinations
    /// </summary>
    /// <returns>High-volume test scenarios</returns>
    public static IEnumerable<object[]> GenerateStressTestCases()
    {
        var stressTests = new List<object[]>();
        
        // Rapid sequential requests
        for (int i = 0; i < 10; i++)
        {
            stressTests.Add([$"stress-test-{i}", "Sequential request batch"]);
        }
        
        return stressTests;
    }

    #endregion
}
