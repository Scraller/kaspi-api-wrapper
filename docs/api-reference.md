# 📖 API Reference - Kaspi.kz API Wrapper

## Base URL
```
http://localhost:5000/api
```

## Authentication
Currently, no authentication is required. All endpoints are publicly accessible.

## Response Format

All API responses follow a standardized format:

```json
{
  "success": boolean,
  "message": "string",
  "data": object|array,
  "errors": ["string"],
  "timestamp": "2025-01-27T10:30:00Z",
  "metadata": {
    "totalCount": number,
    "page": number,
    "pageSize": number,
    "additionalInfo": object
  }
}
```

## Categories API

### Get All Categories

Retrieve all available categories with hierarchical structure.

**Endpoint:** `GET /api/categories/kaspi`

**Parameters:**
- `includeSubcategories` (boolean, optional) - Include subcategories in response (default: true)

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi?includeSubcategories=true" \
     -H "accept: application/json"
```

**Example Response:**
```json
{
  "success": true,
  "message": "Retrieved 1970 categories successfully",
  "data": [
    {
      "name": "Electronics",
      "slug": "electronics",
      "url": "https://kaspi.kz/shop/c/electronics/",
      "level": 0,
      "parentSlug": null,
      "subcategories": [
        {
          "name": "Smartphones",
          "slug": "smartphones",
          "url": "https://kaspi.kz/shop/c/smartphones/",
          "level": 1,
          "parentSlug": "electronics",
          "subcategories": []
        }
      ]
    }
  ],
  "metadata": {
    "totalCount": 1970,
    "page": 1,
    "pageSize": 1970
  }
}
```

### Get Top-Level Categories

Retrieve only top-level categories without subcategories.

**Endpoint:** `GET /api/categories/kaspi/top-level`

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/top-level" \
     -H "accept: application/json"
```

### Get Subcategories

Retrieve subcategories for a specific parent category.

**Endpoint:** `GET /api/categories/kaspi/{parentSlug}/subcategories`

**Parameters:**
- `parentSlug` (string, required) - URL slug of the parent category

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/electronics/subcategories" \
     -H "accept: application/json"
```

### Search Categories

Search categories by name or slug.

**Endpoint:** `GET /api/categories/kaspi/search`

**Parameters:**
- `query` (string, required) - Search term (minimum 2 characters)
- `includeSubcategories` (boolean, optional) - Include subcategories in results (default: false)

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/search?query=phone&includeSubcategories=false" \
     -H "accept: application/json"
```

### Scan Category Subcategories

Scan a specific category page to extract its subcategories.

**Endpoint:** `GET /api/categories/kaspi/{parentSlug}/scan-subcategories`

**Parameters:**
- `parentSlug` (string, required) - URL slug of the parent category
- `maxDepth` (integer, optional) - Maximum depth to scan (default: 3, max: 5)
- `recursive` (boolean, optional) - Whether to recursively scan subcategories (default: false)

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/electronics/scan-subcategories?maxDepth=3&recursive=true" \
     -H "accept: application/json"
```

### Bulk Scan Subcategories

Scan multiple categories for their subcategories in bulk.

**Endpoint:** `GET /api/categories/kaspi/bulk-scan-subcategories`

**Parameters:**
- `parentSlugs` (string, required) - Comma-separated list of parent category slugs (max 10)
- `maxDepth` (integer, optional) - Maximum depth to scan (default: 2)
- `recursive` (boolean, optional) - Whether to use recursive scanning (default: false)

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/bulk-scan-subcategories?parentSlugs=electronics,fashion,home&maxDepth=2" \
     -H "accept: application/json"
```

### Debug Category Slugs

Get debug information about available category slugs and names.

**Endpoint:** `GET /api/categories/kaspi/debug/slugs`

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/debug/slugs" \
     -H "accept: application/json"
```

## HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 400 | Bad Request - Invalid parameters |
| 404 | Not Found - Resource not found |
| 429 | Too Many Requests - Rate limited |
| 500 | Internal Server Error |

## Rate Limiting

The API implements rate limiting to prevent abuse:
- **Minimum interval**: 10 seconds between fresh API calls
- **Cache duration**: 30 minutes for category data
- **Rate limit response**: HTTP 429 with retry information

## Error Handling

Errors are returned in the standardized response format:

```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "errors": [
    "Detailed error message",
    "Additional error details"
  ],
  "timestamp": "2025-01-27T10:30:00Z"
}
```

## Data Models

### HierarchicalCategoryInfo

```json
{
  "name": "string",           // Category display name
  "slug": "string",           // URL-safe category identifier
  "url": "string",            // Full URL to category page
  "level": 0,                 // Hierarchy level (0 = top-level)
  "parentSlug": "string",     // Parent category slug (null for top-level)
  "subcategories": []         // Array of child categories
}
```

### ResponseMetadata

```json
{
  "totalCount": 100,          // Total number of items
  "page": 1,                  // Current page number
  "pageSize": 50,             // Items per page
  "additionalInfo": {}        // Extra metadata specific to endpoint
}
```

## Examples

### Complete Category Exploration

```bash
# 1. Get all top-level categories
curl "http://localhost:5000/api/categories/kaspi/top-level"

# 2. Get subcategories for electronics
curl "http://localhost:5000/api/categories/kaspi/electronics/subcategories"

# 3. Search for phone-related categories
curl "http://localhost:5000/api/categories/kaspi/search?query=phone"

# 4. Get detailed scan of electronics category
curl "http://localhost:5000/api/categories/kaspi/electronics/scan-subcategories?recursive=true&maxDepth=3"
```

### Working with Different Response Formats

```bash
# Get categories without subcategories
curl "http://localhost:5000/api/categories/kaspi?includeSubcategories=false"

# Search with subcategories included
curl "http://localhost:5000/api/categories/kaspi/search?query=electronics&includeSubcategories=true"
```

## Future Endpoints (Coming Soon)

- **Products API**: Search and retrieve product information
- **Merchants API**: Get merchant profiles and ratings
- **Regions API**: Access regional pricing and availability data
- **Reviews API**: Product reviews and ratings

## SDKs and Libraries

Currently, we don't provide official SDKs, but the API is REST-compliant and can be consumed by any HTTP client library:

- **.NET**: HttpClient, RestSharp
- **JavaScript**: fetch, axios
- **Python**: requests, httpx
- **PHP**: Guzzle, cURL
- **Java**: OkHttp, Apache HttpClient

## Support

- 📖 **Documentation**: [docs/](../docs/)
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/Scraller/kaspi-api-wrapper/issues)
- 💡 **Feature Requests**: [GitHub Issues](https://github.com/Scraller/kaspi-api-wrapper/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/Scraller/kaspi-api-wrapper/discussions)
