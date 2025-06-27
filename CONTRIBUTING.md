# Contributing to Kaspi.kz API Wrapper

Thank you for your interest in contributing to the Kaspi.kz API Wrapper! 🎉

## 🤝 How to Contribute

### 1. Fork and Clone
```bash
git clone https://github.com/Scraller/kaspi-api-wrapper.git
cd kaspi-api-wrapper
```

### 2. Set Up Development Environment
```bash
# Backend setup
cd backend
dotnet restore ProductSearchEngine.sln
dotnet build ProductSearchEngine.sln

# Run tests to ensure everything works
dotnet test
```

### 3. Create a Feature Branch
```bash
git checkout -b feature/your-feature-name
```

### 4. Development Guidelines

#### Code Style
- Follow .NET coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new functionality

#### API Development
- All controllers must follow the established patterns in `CategoriesController`
- Use `ApiResponse<T>` wrapper for all responses
- Include comprehensive Swagger documentation
- Handle errors gracefully with proper HTTP status codes

#### Testing Requirements
- Write BDD scenarios for new features using Reqnroll
- Add unit tests with minimum 80% code coverage
- Include integration tests for API endpoints
- Use the established test patterns in the project

### 5. Commit Your Changes
```bash
git add .
git commit -m "feat: add new feature description"
```

### 6. Submit a Pull Request
- Push your branch to your fork
- Create a PR with a clear description
- Reference any related issues
- Ensure all tests pass

## 🛡️ Code of Conduct

### Our Standards
- Be respectful and inclusive
- Welcome newcomers and help them learn
- Focus on constructive feedback
- Keep discussions professional

### Unacceptable Behavior
- Harassment or discrimination
- Trolling or inflammatory comments
- Publishing others' private information
- Any conduct that violates GitHub's Terms of Service

## 🐛 Reporting Issues

### Bug Reports
When reporting bugs, please include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or error messages

### Feature Requests
For new features, please include:
- Clear description of the proposed feature
- Use case and motivation
- Possible implementation approach
- Impact on existing functionality

## 📚 Development Resources

### Project Structure
```
backend/
├── ProductSearchEngine.Api/          # REST API controllers
├── ProductSearchEngine.Scraper/      # Kaspi API clients
├── ProductSearchEngine.Tests/        # BDD & unit tests
└── ProductSearchEngine.Domain/       # Business models
```

### Key Patterns
- **Controllers**: Use dependency injection, ApiResponse wrapper
- **API Clients**: Follow KaspiNavigationApiClient patterns
- **Testing**: BDD scenarios + unit tests + integration tests
- **Documentation**: XML docs + Swagger annotations

### Useful Commands
```bash
# Run specific test category
dotnet test --filter "Category=BDD"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format code
dotnet format

# Start API server
dotnet run --project ProductSearchEngine.Api
```

## 🏆 Recognition

Contributors will be:
- Listed in the project README
- Credited in release notes
- Given appropriate GitHub repository permissions
- Invited to join our contributor community

## 📞 Getting Help

- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For general questions and ideas
- **Code Review**: All PRs receive thorough review and feedback

Thank you for helping make Kaspi.kz data more accessible to developers! 🚀
