Feature: Kaspi Navigation API Client
    As a system administrator
    I want to retrieve category hierarchies from Kaspi's navigation APIs
    So that I can ensure the application has up-to-date product categories

Background:
    Given the Kaspi Navigation API client is configured
    And the HTTP client is set up with proper headers and anti-detection measures

Scenario: Retrieve basic category hierarchy successfully
    When I request the basic category hierarchy with depth 1
    Then the navigation API response should be successful
    And the category list should contain main categories only
    And each category should have a valid name and slug
    And categories should be organized in hierarchical levels

Scenario: Retrieve expanded category hierarchy with subcategories
    When I request the expanded category hierarchy with depth 3
    Then the navigation API response should be successful
    And the category list should contain categories with subcategories
    And each category should have a valid name and slug
    And categories should be organized in hierarchical levels

Scenario: Retrieve categories with different depth levels
    When I request categories with depth <depth>
    Then the navigation API response should be successful
    And the maximum category level should not exceed <expected_max_level>
    And the navigation API response time should be under 15 seconds

    Examples:
      | depth | expected_max_level |
      | 1     | 0                  |
      | 2     | 1                  |
      | 3     | 2                  |

Scenario: Retrieve categories for different cities
    When I request categories for city "<city_id>" named "<city_name>"
    Then the navigation API response should be successful
    And the category list should contain categories
    And the navigation API response time should be under 15 seconds

    Examples:
      | city_id   | city_name   |
      | 750000000 | Almaty      |
      | 710000000 | Nur-Sultan  |

Scenario: Performance test with multiple consecutive requests
    Given I want to perform a performance test with 3 iterations
    When I make consecutive requests to the navigation API
    Then all requests should be successful
    And the average response time should be under 10 seconds
    And no request should take longer than 15 seconds
    And the category count should be consistent across requests

Scenario: Handle API errors gracefully
    Given the navigation API is temporarily unavailable
    When I request the category hierarchy from the navigation API
    Then the client should handle the error gracefully
    And appropriate error logging should be performed

Scenario: Validate category data structure
    Given I have successfully retrieved categories from the API
    When I examine the category data structure
    Then each category should have required properties
    And category levels should be sequential starting from 0
    And category slugs should be URL-safe
    And parent-child relationships should be consistent

Scenario: Test anti-detection measures
    Given I make multiple rapid requests to the API
    When the anti-detection system is active
    Then requests should be throttled appropriately
    And proper headers should be sent with each request
    And session management should be maintained

Scenario: Verify category parsing from different API endpoints
    When I fetch categories from the menu API endpoint
    And I fetch categories from the footer API endpoint
    Then both endpoints should be processed successfully
    And the combined category list should contain all unique categories
    And duplicate categories should be handled properly
