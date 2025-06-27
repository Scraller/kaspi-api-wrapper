# 🚀 Getting Started with Kaspi.kz API Wrapper

Welcome to the Kaspi.kz API Wrapper! This guide will help you get up and running quickly.

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

### Required
- **.NET 9.0 SDK** or later
  - Download: https://dotnet.microsoft.com/download/dotnet/9.0
  - Verify installation: `dotnet --version`

### Optional (for contributors)
- **Git** - For version control
- **Docker** - For containerized deployment
- **Visual Studio Code** or **Visual Studio** - For development

## ⚡ Quick Start (5 minutes)

### 1. Clone and Build
```bash
# Clone the repository
git clone https://github.com/Scraller/kaspi-api-wrapper.git
cd kaspi-api-wrapper

# Navigate to backend
cd backend

# Restore dependencies
dotnet restore ProductSearchEngine.sln

# Build the project
dotnet build ProductSearchEngine.sln
```

### 2. Run the API
```bash
# Start the API server
dotnet run --project ProductSearchEngine.Api

# The API will be available at:
# - API: http://localhost:5000
# - Swagger UI: http://localhost:5000/swagger
```

### 3. Test Your First API Call
Open your browser or use curl:

```bash
# Get all categories
curl http://localhost:5000/api/categories/kaspi

# Search for electronics
curl "http://localhost:5000/api/categories/kaspi/search?query=electronics"
```

## 🐳 Docker Quick Start

If you prefer using Docker:

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or build and run manually
docker build -t kaspi-api-wrapper -f backend/Dockerfile .
docker run -p 5000:8080 kaspi-api-wrapper
```

## 📚 Next Steps

### Explore the API
1. **Swagger Documentation**: Visit http://localhost:5000/swagger
2. **Try different endpoints**:
   - Categories: `/api/categories/kaspi`
   - Search: `/api/categories/kaspi/search?query=phone`
   - Subcategories: `/api/categories/kaspi/electronics/subcategories`

### Development
1. **Read the [API Documentation](api-reference.md)**
2. **Check out [Contributing Guidelines](../CONTRIBUTING.md)**
3. **Run tests**: `dotnet test`
4. **View test coverage**: `dotnet test --collect:"XPlat Code Coverage"`

## 🔧 Configuration

### Environment Variables
The API can be configured using environment variables:

```bash
# Logging level
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Information

# API settings (if needed)
export AllowedHosts=*
```

### appsettings.json
You can modify `backend/ProductSearchEngine.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 🧪 Running Tests

```bash
# All tests
dotnet test

# Specific test categories
dotnet test --filter "Category=BDD"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Unit"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🛠️ Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find process using port 5000
lsof -ti:5000

# Kill the process
kill -9 <PID>

# Or run on different port
dotnet run --project ProductSearchEngine.Api --urls "http://localhost:5001"
```

#### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore --force
dotnet build
```

#### Test Failures
```bash
# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Getting Help

- 📖 **Documentation**: Check the [docs](../docs/) folder
- 🐛 **Issues**: [GitHub Issues](https://github.com/Scraller/kaspi-api-wrapper/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/Scraller/kaspi-api-wrapper/discussions)

## 🎯 What's Next?

- **Explore API Endpoints**: Try all available endpoints in Swagger
- **Read the Documentation**: Understand the project architecture
- **Contribute**: Help us add new features and fix bugs
- **Join the Community**: Participate in discussions and share feedback

Happy coding! 🚀
