# LEGACY FEATURE - DISABLED
# This feature is now legacy since we migrated to API endpoints instead of HTML scraping
# Tests are ignored to prevent them from running but kept for reference

@ignore
Feature: Kaspi Categories Page Scanner
    As a backend validation service
    I want to scan and validate Kaspi.kz category hierarchy
    So that I can ensure data integrity and proper category structure

Background:
    Given the categories page scanner is initialized
    And the scanner is configured with anti-detection headers

Scenario: Successfully scan categories page and extract hierarchy
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And I should receive a non-empty list of categories
    And all categories should have valid names
    And all categories should have valid URLs
    And all categories should have valid slugs

Scenario: Validate category hierarchy structure
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And I should receive both parent and subcategories
    And each subcategory should have a valid parent
    And parent categories should have proper subcategory relationships if subcategories exist
    And the hierarchy depth should not exceed 2 levels

Scenario: Validate product count consistency
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And the total product count should equal the sum of all category product counts
    And each parent category product count should equal the sum of its subcategories product counts
    And all product counts should be positive numbers

Scenario: Exclude summary and navigation categories
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And the results should not contain "Все категории"
    And the results should not contain navigation links
    And all returned categories should be actual product categories

Scenario: Validate category URL patterns
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And all category URLs should follow the kaspi.kz pattern
    And all category URLs should contain "/shop/c/"
    And all category slugs should be URL-encoded properly
    And no duplicate URLs should exist

Scenario: Handle network and parsing errors gracefully
    Given the scanner encounters a network error
    When I scan the Kaspi categories page
    Then the scanner should handle the error gracefully
    And return an empty list without throwing exceptions

Scenario: Validate minimum expected categories
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And I should receive at least 20 top-level categories
    And I should receive at least 50 subcategories
    And the total category count should be at least 70

Scenario: Validate specific major categories exist
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And the results should contain "Одежда" category
    And the results should contain "Компьютеры" category
    And the results should contain "Красота и здоровье" category
    And the results should contain "Автотовары" category
    And the results should contain "Телефоны и гаджеты" category

Scenario: Validate category data completeness
    When I scan the Kaspi categories page
    Then the scanning should complete successfully
    And every category should have a non-empty name
    And every category should have a valid product count
    And every category should have a properly formatted URL
    And every category should have a valid slug
    And every category should have a defined hierarchy level
