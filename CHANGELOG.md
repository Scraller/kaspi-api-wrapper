# Kaspi.kz API Wrapper - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of Kaspi.kz API Wrapper
- Categories API with hierarchical structure
- Category search functionality
- Comprehensive Swagger documentation
- BDD test suite with Reqnroll
- CI/CD pipeline with GitHub Actions
- Docker support for deployment

### API Endpoints
- `GET /api/categories/kaspi` - Get all categories
- `GET /api/categories/kaspi/top-level` - Get top-level categories
- `GET /api/categories/kaspi/{slug}/subcategories` - Get subcategories
- `GET /api/categories/kaspi/search` - Search categories
- `GET /api/categories/kaspi/{slug}/scan-subcategories` - Scan category pages
- `GET /api/categories/kaspi/bulk-scan-subcategories` - Bulk subcategory scanning

### Technical Features
- Anti-detection and rate limiting
- Standardized `ApiResponse<T>` wrapper
- Comprehensive error handling
- Structured logging
- XML documentation
- 80%+ test coverage

## [1.0.0] - 2025-01-27

### Added
- Initial public release
- Open-source licensing (MIT)
- Community contribution guidelines

---

## Template for Future Releases

## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security vulnerability fixes
