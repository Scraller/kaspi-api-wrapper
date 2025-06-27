Feature: Product Specifications and Descriptions API
    As a developer using the Kaspi API wrapper
    I want to retrieve detailed product specifications and descriptions
    So that I can display comprehensive product information in my application

Background:
    Given the Product Specifications API client is configured
    And the product specifications API client is set up with proper headers and anti-detection measures

# Scenario: Products with complete specifications and descriptions
Scenario: Retrieve product with full specifications and description
    Given a product ID "129349158" with complete specifications data
    When I request product specifications and description
    Then the product specifications response should be successful
    And the product should have a valid description
    And the product should have specification groups OR be gracefully handled when missing
    And the response should contain product basic information
    And the product specifications response time should be under 8 seconds

# Scenario: Products with minimal specifications
Scenario: Retrieve product with minimal specifications
    Given a product ID "102298404" with limited specifications data
    When I request product specifications and description
    Then the product specifications response should be successful
    And the product should have a valid description
    And the product may have empty specification groups
    And missing specifications should be handled gracefully
    And the response should contain product basic information

# Scenario: Products without specifications (due to current API implementation)
## Bad test, since Kaspi.kz require to have specifications
@ignore
Scenario: Retrieve product without specifications
    Given a product ID "102298404" with no specifications data
    When I request product specifications and description
    Then the product specifications response should be successful
    And the product should have empty specifications list
    And the product description may be empty or null
    And the response should contain product basic information
    And no errors should be returned for missing specifications

# Scenario: Non-existent product handling
Scenario: Handle non-existent product gracefully
    Given a product ID "999999999" that does not exist
    When I request product specifications and description
    Then the response should return not found
    And the error message should indicate product not found
    And the response should be properly formatted

# Scenario: Invalid product ID handling
Scenario: Handle invalid product ID gracefully
    Given an invalid product ID "invalid123" for specifications
    When I request product specifications and description
    Then the response should return not found
    And the error message should indicate product not found
    And the response should be properly formatted

# Scenario: Network error handling
@ignore
Scenario: Handle network errors gracefully
    Given a product ID "129349158" but network is unavailable
    When I request product specifications and description
    Then the response should return server error
    And the error message should indicate network failure
    And the response should be properly formatted

# Scenario: Regional specifications differences
Scenario Outline: Retrieve product specifications for different regions
    Given a product ID "129349158" with complete specifications data
    And a city code "<cityCode>" for "<cityName>"
    When I request product specifications and description with regional parameters
    Then the product specifications response should be successful
    And the product should have region-specific pricing
    And the specifications should be consistent across regions
    And the description should be consistent across regions

    Examples:
      | cityCode  | cityName |
      | 750000000 | Алматы   |
      | 710000000 | Астана   |

# Scenario: Specification groups structure validation
Scenario: Validate specification groups structure
    Given a product ID "129349158" with complete specifications data
    When I request product specifications and description
    Then the product specifications response should be successful
    And each specification group should have a code and name
    And each specification group should have a features list
    And each feature should have a name and values
    And feature values should be non-empty strings
    And specification codes should follow expected patterns

# Scenario: Multi-valued features handling
Scenario: Handle multi-valued specification features
    Given a product ID "102298404" with multi-valued features
    When I request product specifications and description
    Then the product specifications response should be successful
    And multi-valued features should contain multiple values
    And each value should be properly formatted
    And the multiValued flag should be set correctly

# Scenario: Specification types validation
Scenario: Validate different specification types
    Given a product ID "129349158" with various specification types
    When I request product specifications and description
    Then the product specifications response should be successful
    And ENUM type specifications should be handled correctly
    And STRING type specifications should be handled correctly
    And NUMBER type specifications should be handled correctly
    And unknown types should be handled gracefully

# Scenario: Large description text handling
Scenario: Handle products with large description text
    Given a product ID "129349158" with extensive description
    When I request product specifications and description
    Then the product specifications response should be successful
    And the complete description text should be returned
    And the description should be properly encoded
    And the response size should be reasonable

# Scenario: HTML content in specifications
Scenario: Handle HTML content in specifications safely
    Given a product ID with HTML content in specifications
    When I request product specifications and description
    Then the product specifications response should be successful
    And HTML content should be properly escaped or sanitized
    And no script injection should be possible
    And the data should be safe for frontend display

# Scenario: Concurrent requests handling
Scenario: Handle multiple concurrent specification requests
    Given multiple product IDs with specifications data
    When I send concurrent requests for product specifications
    Then all responses should be successful
    And each response should contain correct product data
    And no rate limiting errors should occur
    And response times should remain reasonable

# Scenario: Image gallery extraction
Scenario: Extract product image gallery with specifications
    Given a product ID "129349158" with image gallery
    When I request product specifications and description
    Then the product specifications response should be successful
    And the product should have gallery images
    And each image should have small, medium, and large URLs
    And image URLs should be valid and accessible
    And image metadata should be included

# Scenario: Performance requirements validation
Scenario: Validate API performance requirements
    Given a product ID "129349158" with complete specifications data
    When I request product specifications and description
    Then the response should be under 3 seconds
    And the response payload should be under 1MB
    And the API should handle 100 requests per minute
    And memory usage should remain stable
