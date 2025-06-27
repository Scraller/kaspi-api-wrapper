# 🛍️ Kaspi.kz API Wrapper

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![API](https://img.shields.io/badge/API-REST-blue.svg)](https://swagger.io/)

An **unofficial** open-source REST API wrapper for Kaspi.kz marketplace, providing clean, documented access to product categories, search, pricing, and merchant data through standardized endpoints.

## 🎯 Features

### 🏪 Kaspi.kz API Access
- **Category Management** - Access 1,970+ product categories with hierarchical structure
- **Product Search** - Search products by name, category, price range, and specifications
- **Merchant Profiles** - Get detailed merchant information and ratings  
- **Product Details** - Fetch comprehensive product data including specifications
- **Regional Data** - Access region-specific pricing and availability

### 🛠️ Developer Experience
- **Swagger Documentation** - Interactive API documentation
- **Rate Limiting** - Built-in request throttling and anti-detection
- **Error Handling** - Comprehensive error responses with proper HTTP status codes
- **Response Standardization** - Consistent `ApiResponse<T>` wrapper for all endpoints
- **Async/Await** - Modern async patterns throughout

### 🚀 Production Ready
- **Comprehensive Testing** - BDD scenarios, unit tests, integration tests
- **Docker Support** - Containerized deployment ready
- **Logging** - Structured logging with configurable levels
- **Health Checks** - Built-in health monitoring endpoints

## 🏗️ Architecture

This project follows clean architecture principles with clear separation of concerns:

### Backend (.NET 9)
```
backend/
├── ProductSearchEngine.Api/          # REST API controllers & endpoints
├── ProductSearchEngine.Scraper/      # Kaspi API clients & web scraping
└── ProductSearchEngine.Tests/        # BDD, unit & integration tests
```

### Key Components
- **API Controllers** - RESTful endpoints with Swagger documentation
- **API Clients** - Kaspi.kz web scraping and data extraction
- **Response Models** - Standardized data transfer objects
- **Testing Suite** - Comprehensive test coverage with BDD scenarios

## 🚀 Quick Start

### Prerequisites
- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Git** - For cloning the repository
- **Docker** (optional) - For containerized deployment

### 🔧 Installation

1. **Clone the repository**
```bash
git clone https://github.com/Scraller/kaspi-api-wrapper.git
cd kaspi-api-wrapper
```

2. **Build and run the API**
```bash
cd backend
dotnet restore ProductSearchEngine.sln
dotnet build ProductSearchEngine.sln
dotnet run --project ProductSearchEngine.Api
```

3. **Access the API**
- **Swagger UI**: http://localhost:5147/swagger

### 📚 API Usage Examples

#### Get all categories
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi" \
     -H "accept: application/json"
```

#### Search for products
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/search?query=smartphone" \
     -H "accept: application/json"
```

#### Get category subcategories  
```bash
curl -X GET "http://localhost:5000/api/categories/kaspi/electronics/subcategories" \
     -H "accept: application/json"
```

### Running Tests

#### All Tests with Coverage
```bash
cd backend
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

#### BDD Tests (Gherkin scenarios)
```bash
dotnet test --filter "Category=BDD"
```

#### Integration Tests
```bash
dotnet test --filter "Category=Integration"
```

#### Unit Tests Only
```bash
dotnet test --filter "Category=Unit"
```

### 📊 Test Coverage
The project maintains high test coverage with:
- **BDD Scenarios** - Business-readable feature tests
- **Integration Tests** - API endpoint testing
- **Unit Tests** - Component-level testing

## 📖 API Documentation

### 🌐 Interactive Documentation
Once the API is running, visit **http://localhost:5000/swagger** for interactive API documentation.

### 📂 Available Endpoints

#### Categories API
- `GET /api/categories/kaspi` - Get all categories with hierarchy
- `GET /api/categories/kaspi/top-level` - Get top-level categories only  
- `GET /api/categories/kaspi/{slug}/subcategories` - Get subcategories
- `GET /api/categories/kaspi/search?query={term}` - Search categories

#### Future Endpoints (Coming Soon)
- `GET /api/products/search` - Product search
- `GET /api/products/{id}` - Product details
- `GET /api/merchants/{id}` - Merchant profiles
- `GET /api/regions` - Regional data

### 📝 Response Format
All API responses use a standardized format:
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": [...],
  "errors": [],
  "timestamp": "2025-01-27T10:30:00Z",
  "metadata": {
    "totalCount": 100,
    "page": 1,
    "pageSize": 50
  }
}
```
  So that I can track price changes

Scenario: Scrape smartphone products
  Given I have a Kaspi scraper configured
  When I search for "smartphone" products
  Then I should receive at least 10 products
  And each product should have a valid price
  And each product should have a description
```

## 🔧 Configuration

### Scraper Configuration
```json
{
  "Scrapers": {
    "Kaspi": {
      "MaxConcurrentRequests": 5,
      "RequestDelayMs": 1000,
      "UserAgents": [...],
      "CityIds": ["750000000", "710000000"]
    }
  }
}
```

### Test Categories
- **Unit**: Fast, isolated component tests
- **BDD**: Business scenario validation
- **Integration**: End-to-end workflow tests
- **Performance**: Load and stress testing

## 🛠️ Tech Stack

### Backend
- **.NET 9**
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **NUnit** - Unit testing framework
- **Reqnroll** - BDD testing
- **Serilog** - Structured logging

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### 🚀 Quick Contribution Steps
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes and add tests
4. Run tests: `dotnet test`
5. Commit changes: `git commit -m "feat: add my feature"`
6. Push to branch: `git push origin feature/my-feature`
7. Submit a Pull Request

### 🎯 Areas for Contribution
- 🆕 **New API endpoints** (Products, Merchants, Search)
- 🧪 **Additional test coverage**
- 📖 **Documentation improvements**
- 🐛 **Bug fixes and optimizations**
- 🔧 **Performance enhancements**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚖️ Legal Notice

**This is an unofficial API wrapper.** This project:
- ✅ Uses publicly available data from Kaspi.kz
- ✅ Respects rate limits and website terms
- ✅ Does not store or expose personal user data
- ❌ Is not affiliated with or endorsed by Kaspi.kz

**Use responsibly** and in accordance with Kaspi.kz's terms of service.

## 🛡️ Security

Security is important to us. If you discover a security vulnerability, please review our [Security Policy](SECURITY.md) for responsible disclosure guidelines.

## 📊 Project Status

### ✅ Current Features
- ✅ Categories API with hierarchical structure
- ✅ Category search functionality
- ✅ Comprehensive test suite
- ✅ Swagger documentation
- 🔄 Products API endpoints
- 🔄 Merchant profiles API
- 🔄 Advanced search filters
- 🔄 Performance optimizations


## 📞 Support & Community

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/Scraller/kaspi-api-wrapper/issues)
- 💡 **Feature Requests**: [GitHub Issues](https://github.com/Scraller/kaspi-api-wrapper/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/Scraller/kaspi-api-wrapper/discussions)
- 📖 **Documentation**: [API Docs](https://kaspi-api-docs.dev)

## 🙏 Acknowledgments

- **Kaspi.kz** - For providing an excellent e-commerce platform
- **Open Source Community** - For the amazing tools and libraries
- **Contributors** - Everyone who helps improve this project

## 📈 Stats

![GitHub stars](https://img.shields.io/github/stars/Scraller/kaspi-api-wrapper?style=social)
![GitHub forks](https://img.shields.io/github/forks/Scraller/kaspi-api-wrapper?style=social)
![GitHub issues](https://img.shields.io/github/issues/Scraller/kaspi-api-wrapper)
![GitHub pull requests](https://img.shields.io/github/issues-pr/Scraller/kaspi-api-wrapper)

---

**Made with ❤️ by the open-source community**

*Help us make Kaspi.kz data more accessible to developers worldwide!*
