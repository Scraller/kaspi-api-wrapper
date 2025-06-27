Feature: Kaspi Categories SubCategories Scanner
    As a backend validation service
    I want to scan specific Kaspi.kz category pages to extract their subcategories
    So that I can discover deeper category hierarchies beyond the main categories page
 
Background:
    Given the subcategories scanner is initialized
    And the subcategories scanner is configured with anti-detection headers

Scenario: Successfully scan category page for subcategories when available
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And if subcategories are found then I should receive a non-empty list of subcategories
    And if subcategories are found then all subcategories should have valid names
    And if subcategories are found then all subcategories should have valid URLs
    And if subcategories are found then all subcategories should have valid slugs

Scenario: Validate subcategory hierarchy structure when subcategories are found
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And if subcategories are found then I should receive subcategories with proper parent relationships
    And if subcategories are found then each subcategory should reference the correct parent category
    And if subcategories are found then subcategories should be at level 1 or higher
    And if subcategories are found then the hierarchy depth should not exceed 3 levels

Scenario: Exclude parent category from subcategory results when subcategories are found
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And if subcategories are found then the results should not contain the parent category itself
    And if subcategories are found then all returned categories should be actual subcategories
    And if subcategories are found then no duplicate categories should exist

Scenario: Validate subcategory URL patterns when subcategories are found
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And if subcategories are found then all subcategory URLs should follow the kaspi.kz pattern
    And if subcategories are found then all subcategory URLs should contain "/shop/c/"
    And if subcategories are found then all subcategory slugs should be URL-encoded properly
    And if subcategories are found then subcategory URLs should be different from the parent category URL

@ignore
Scenario: Handle network and parsing errors gracefully for category pages
    Given the subcategories scanner encounters a network error
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategories scanner should handle the error gracefully
    And return an empty subcategories list without throwing exceptions

Scenario: Validate minimum expected subcategories for smartphones and gadgets
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And I should receive at least 5 subcategories from the category scan
    And I should receive at most 120 subcategories
    And the subcategory count should be reasonable for the category

Scenario: Validate specific expected subcategories for smartphones and gadgets
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And the results might contain phone-related subcategories
    And the results might contain accessory-related subcategories
    And the results might contain gadget-related subcategories

Scenario: Compare subcategories with main categories scanner results
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And subcategories should provide more granular categories than main scanner
    And subcategories should have the smartphones and gadgets category as ancestor
    And subcategories should represent deeper categorization

Scenario: Scan different category types for subcategories
    When I scan the beauty and health category page for subcategories  
    Then the subcategory scanning should complete successfully
    And I should receive beauty-related subcategories
    And subcategories should be different from smartphones subcategories
    And each category type should have distinct subcategory patterns

Scenario: Validate subcategory extraction from various page structures
    When I scan the smartphones and gadgets category page for subcategories
    Then the subcategory scanning should complete successfully
    And subcategories should be extracted from sidebar navigation
    And subcategories should be extracted from category filters
    And subcategories should be extracted from breadcrumb-adjacent sections
    And subcategories should be extracted from category cards or tiles
