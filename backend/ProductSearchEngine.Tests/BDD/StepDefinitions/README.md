# BDD Step Definitions Organization

This folder contains the BDD (Behavior-Driven Development) step definitions organized by feature domain.

## Folder Structure

### OfferApiClient/
Contains step definitions for testing the Kaspi Offer API Client functionality:
- **BaseStepDefinitions.cs** - Base class with shared state and common utilities
- **SetupStepDefinitions.cs** - Setup and configuration step definitions (Given steps)
- **BasicStepDefinitions.cs** - Basic API operations (When steps for simple fetching)
- **DynamicStepDefinitions.cs** - Dynamic product testing using real scraped data (When steps for complex scenarios)
- **ValidationStepDefinitions.cs** - Validation and assertion step definitions (Then steps)

### ProductScraping/
Contains step definitions for testing product scraping functionality:
- **ProductScrapingStepDefinitions.cs** - Product scraping operations and validations

### CategoriesScanning/
Contains step definitions for testing category scanning functionality:
- **CategoriesPageScannerStepDefinitions.cs** - Category page scanning operations and validations

## Legacy Files

- **KaspiOfferApiClientStepDefinitions.cs** - Original monolithic step definitions file (930+ lines)
  - This file should be removed after confirming all functionality has been properly migrated to the new organized structure

## Benefits of the New Structure

1. **Better Organization** - Related step definitions are grouped together by feature domain
2. **Improved Maintainability** - Smaller, focused files are easier to maintain and understand
3. **Clear Separation of Concerns** - Each file has a specific responsibility (setup, operations, validations, etc.)
4. **Easier Navigation** - Developers can quickly find the relevant step definitions for their work
5. **Scalability** - New features can be added in appropriate folders without cluttering the main directory

## Namespace Convention

Each subfolder uses its own namespace:
- `ProductSearchEngine.Tests.BDD.StepDefinitions.OfferApiClient`
- `ProductSearchEngine.Tests.BDD.StepDefinitions.ProductScraping`
- `ProductSearchEngine.Tests.BDD.StepDefinitions.CategoriesScanning`
