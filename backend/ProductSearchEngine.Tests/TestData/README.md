# API Test Data Matrix Documentation

## Overview

This document describes the comprehensive test data matrix system designed for testing the Kaspi.kz API Wrapper with extensive edge cases, validation scenarios, and data-driven testing approaches.

## Structure

### 📁 Test Data Organization

```
ProductSearchEngine.Tests/
├── TestData/
│   └── ApiTestDataMatrix.cs          # Comprehensive test data definitions
├── Integration/
│   ├── DataDriven/                   # Data-driven integration tests
│   │   ├── ProductsControllerDataDrivenTests.cs
│   │   ├── RegionsControllerDataDrivenTests.cs
│   │   └── SearchControllerDataDrivenTests.cs
│   └── Swagger/                      # Original Swagger-focused tests
│       ├── ProductsControllerSwaggerTests.cs
│       ├── RegionsControllerSwaggerTests.cs
│       └── [Other Controller Tests]
└── Unit/                             # Unit tests (if applicable)
```

## 🎯 Test Data Matrix Categories

### 1. Product Search Test Data

**Endpoint**: `GET /api/Products/search`
- **Required**: `text` (string)
- **Optional**: `category` (string), `cityCode` (string), `page` (int), `pageSize` (int)

#### Test Cases Include:
- ✅ **Happy Path**: Valid searches with popular products
- ✅ **Edge Cases**: Minimum/maximum values, special characters
- ❌ **Invalid Cases**: Empty text, invalid pagination, out-of-range values
- 🌐 **Multilingual**: Cyrillic, Latin, mixed scripts
- 🏙️ **Regional**: Different city codes
- 📱 **Categories**: Valid and invalid category filters

**Example Test Data**:
```csharp
// Valid case
{ "iPhone", null, "750000000", 1, 20, true, "Basic search with popular product" }

// Edge case
{ "ab", null, "750000000", 1, 1, true, "Minimum valid search length" }

// Invalid case
{ "", null, "750000000", 1, 20, false, "Empty search text" }
```

### 2. Product Details Test Data

**Endpoint**: `GET /api/Products/{productId}`
- **Required**: `productId` (string)
- **Optional**: `cityCode` (string)

#### Test Cases Include:
- ✅ **Valid Product IDs**: Known existing products
- ❌ **Invalid Product IDs**: Non-existent, malformed, empty
- 🏙️ **City Variations**: Different city codes, invalid cities
- 🔤 **Format Variations**: Whitespace, special characters

### 3. Regions/Cities Test Data

**Endpoints**: 
- `GET /api/Regions/cities`
- `GET /api/Regions/cities/search`
- `GET /api/Regions/cities/{cityId}`

#### Test Cases Include:
- 🏙️ **Major Cities**: Filter for major cities only
- 🔍 **Search Variations**: Partial names, different languages
- 📊 **Pagination**: Limit validation, boundary testing
- 🌐 **Multilingual**: Cyrillic and Latin city names

### 4. Search Suggestions Test Data

**Endpoint**: `GET /api/Search/suggestions`
- **Required**: `q` (string)
- **Optional**: `limit` (int), `category` (string)

#### Test Cases Include:
- 📝 **Partial Queries**: Incomplete words, autocomplete scenarios
- 📏 **Limit Testing**: Valid and invalid limit values
- 🎯 **Category Filtering**: Valid categories, invalid categories
- 🌐 **Language Mixing**: English, Russian, Kazakh terms

## 🛠️ Usage Examples

### Basic Data-Driven Test

```csharp
[Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.ProductSearchTestCases))]
public async Task SearchProducts_WithVariousInputs_ShouldHandleCorrectly(
    string text, 
    string? category, 
    string? cityCode, 
    int page, 
    int pageSize, 
    bool shouldSucceed, 
    string testDescription)
{
    // Arrange
    var url = BuildSearchUrl(text, category, cityCode, page, pageSize);

    // Act
    var response = await _client.GetAsync(url);

    // Assert
    if (shouldSucceed)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await ValidateSuccessfulResponse(response, testDescription);
    }
    else
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ValidateErrorResponse(response, testDescription);
    }
}
```

### Performance Testing with Data Matrix

```csharp
[Test]
[TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.CommonSearchTerms))]
public async Task SearchProducts_PerformanceTest_ShouldRespondQuickly(string searchTerm)
{
    var stopwatch = Stopwatch.StartNew();
    
    var response = await _client.GetAsync($"/api/Products/search?text={searchTerm}");
    
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
}
```

### Concurrent Testing

```csharp
[Test]
public async Task SearchProducts_ConcurrentRequests_ShouldHandleGracefully()
{
    var tasks = ApiTestDataMatrix.CommonSearchTerms
        .Take(5)
        .Select(term => _client.GetAsync($"/api/Products/search?text={term}"))
        .ToArray();

    var responses = await Task.WhenAll(tasks);
    
    responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
}
```

## 📊 Test Coverage Matrix

### Input Validation Coverage

| Parameter Type | Valid Cases | Invalid Cases | Edge Cases | Special Cases |
|----------------|-------------|---------------|------------|---------------|
| **Search Text** | ✅ Common terms | ❌ Empty, too short | 🔤 Min/max length | 🌐 Unicode, symbols |
| **Product ID** | ✅ Known IDs | ❌ Invalid format | 🔢 Numeric variations | 🔤 Non-numeric |
| **City Code** | ✅ Valid codes | ❌ Invalid codes | 🏙️ All regions | 🔤 Non-numeric |
| **Pagination** | ✅ Normal range | ❌ Negative, zero | 📊 Min/max values | 🔢 Boundary testing |
| **Limits** | ✅ 1-100 range | ❌ 0, >100, negative | 📏 Boundary values | 🔢 Integer limits |

### Response Validation Coverage

| Aspect | Validation Points |
|--------|------------------|
| **Status Codes** | 200, 400, 404, 500 scenarios |
| **Response Structure** | ApiResponse wrapper, metadata presence |
| **Data Integrity** | Non-null required fields, proper types |
| **Pagination** | TotalCount, Page, PageSize accuracy |
| **Performance** | Response time thresholds |
| **Concurrency** | Multiple simultaneous requests |

## 🎯 Test Data Categories

### Common Test Data Arrays

```csharp
// Valid city codes for Kazakhstan
ValidCityCodes = ["750000000", "710000000", "160000000", ...]

// Common search terms in multiple languages
CommonSearchTerms = ["iPhone", "телефон", "Samsung Galaxy", ...]

// Edge case strings for input validation
EdgeCaseStrings = ["", " ", "a", "superlongtext...", "спец символы", ...]

// Invalid numeric inputs
InvalidNumbers = [-1, 0, 101, int.MaxValue, ...]
```

### Multilingual Support

- **English**: iPhone, Samsung, laptop, headphones
- **Russian**: телефон, компьютер, наушники, игры
- **Kazakh**: смартфон, машина, кітап (where applicable)
- **Mixed**: iPhone 15, Samsung Galaxy, MacBook Pro

### Regional Testing

- **Almaty**: 750000000 (primary test city)
- **Nur-Sultan**: 710000000 (capital city)
- **Shymkent**: 160000000 (southern region)
- **Invalid**: 999999999, "invalid", empty string

## 🔧 Adding New Test Data

### Step 1: Define Test Cases Array

```csharp
public static readonly object[][] NewEndpointTestCases = 
{
    new object[] { param1, param2, shouldSucceed, "description" },
    // Add more test cases...
};
```

### Step 2: Create Data-Driven Test Method

```csharp
[Test, TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.NewEndpointTestCases))]
public async Task NewEndpoint_WithVariousInputs_ShouldHandleCorrectly(
    string param1, 
    bool param2, 
    bool shouldSucceed, 
    string testDescription)
{
    // Test implementation
}
```

### Step 3: Add Edge Cases

```csharp
[Test]
[TestCaseSource(typeof(ApiTestDataMatrix), nameof(ApiTestDataMatrix.EdgeCaseStrings))]
public async Task NewEndpoint_WithEdgeCases_ShouldHandleSafely(string edgeCase)
{
    // Edge case testing
}
```

## 📈 Performance Benchmarks

### Expected Response Times

| Endpoint Type | Target Response Time | Maximum Acceptable |
|---------------|---------------------|-------------------|
| **Simple GET** | < 1 second | < 3 seconds |
| **Search** | < 2 seconds | < 5 seconds |
| **Product Details** | < 2 seconds | < 5 seconds |
| **Bulk Operations** | < 5 seconds | < 10 seconds |

### Concurrent Load Testing

- **Light Load**: 5 concurrent requests
- **Medium Load**: 10-20 concurrent requests  
- **Heavy Load**: 50+ concurrent requests (stress testing)

## 🛡️ Security Testing

### Input Sanitization Tests

```csharp
EdgeCaseStrings = [
    "<script>alert('xss')</script>",  // XSS attempts
    "'; DROP TABLE --",               // SQL injection
    "../../etc/passwd",               // Path traversal
    "javascript:alert(1)",            // JavaScript injection
]
```

### Encoding Tests

- URL encoding validation
- Unicode character handling
- Special character preservation
- Multi-byte character support

## 📋 Running the Tests

### Run All Data-Driven Tests

```bash
dotnet test --filter "Category=DataDriven"
```

### Run Specific Controller Tests

```bash
dotnet test --filter "ClassName~ProductsControllerDataDrivenTests"
```

### Run Performance Tests Only

```bash
dotnet test --filter "TestName~PerformanceTest"
```

### Run with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

## 🔍 Test Result Analysis

### Success Criteria

1. **Functional**: All expected behaviors work correctly
2. **Performance**: Response times meet targets
3. **Resilience**: Graceful handling of invalid inputs
4. **Security**: No information leakage or vulnerabilities
5. **Consistency**: Reliable behavior across different inputs

### Common Issues to Watch For

- **Null Reference Exceptions**: Proper null handling
- **Timeout Issues**: Long-running operations
- **Memory Leaks**: Resource cleanup in concurrent tests
- **Encoding Problems**: Unicode and special characters
- **Rate Limiting**: API throttling behavior

## 🎯 Best Practices

### Test Naming

- Use descriptive test names: `GetProduct_WithInvalidId_ShouldReturnNotFound`
- Include test scenario in TestCaseSource descriptions
- Use consistent naming patterns across controllers

### Test Data Organization

- Group related test cases together
- Use meaningful parameter names
- Include both positive and negative test cases
- Test boundary conditions thoroughly

### Assertion Strategy

- Validate HTTP status codes first
- Check response structure and required fields
- Verify business logic correctness
- Test error message quality and consistency

### Performance Considerations

- Set reasonable timeout thresholds
- Test with realistic data volumes
- Monitor resource usage during concurrent tests
- Validate response times under load

This comprehensive test data matrix ensures thorough testing coverage while maintaining maintainable and scalable test code.
