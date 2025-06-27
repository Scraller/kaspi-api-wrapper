Feature: Kaspi Offer API Client
    As a price monitoring system
    I want to fetch detailed offer information from Kaspi's live offer API
    So that I can provide users with comprehensive real-time merchant and pricing data

Background:
    Given the Kaspi Offer API client is initialized
    And the anti-detection strategy is configured
    And the city ID is set to "551010000" (Almaty)

Scenario: Successfully fetch offers for a real product
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then the request should succeed
    And I should receive at least 1 offer
    And each offer should contain valid merchant information
    And each offer should contain pricing details
    And each offer should contain delivery information

Scenario: Validate real offer data structure
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then all offers should have non-empty merchant names
    And all offers should have non-empty merchant IDs
    And all offers should have valid prices greater than 0
    And offers with rating data should have ratings between 1.0 and 5.0
    And offers with review data should have positive review counts

Scenario: Validate real delivery information
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then all offers should have delivery type information
    And offers with delivery duration should have valid duration values
    And offers with Kaspi delivery should have boolean flags
    And offers with delivery costs should have non-negative costs
    And offers with delivery thresholds should have non-negative thresholds

Scenario: Test price diversity across real merchants
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then I should find offers with different prices if multiple offers exist
    And the price difference between cheapest and most expensive should be reasonable
    And all prices should be positive numbers
    And all prices should be in KZT currency

Scenario: Validate real merchant ratings and reviews
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then offers with ratings should have values between 1.0 and 5.0
    And offers with review counts should have non-negative values
    And rating and review data should be consistent when present

Scenario: Test real availability and stock information
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then offers should have availability information
    And offers should have pickup date information when available
    And offers should have located point information

Scenario: Validate generated offer URLs for real offers
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then offers with merchant IDs should have valid URLs
    And offer URLs should contain the product ID
    And offer URLs should contain merchant ID parameters

Scenario: Handle API request failure gracefully
    Given an invalid product ID "999999999"
    When I fetch offers for the product from live API
    Then I should receive an empty offers collection
    And the failure should be logged appropriately

Scenario: Validate comprehensive offer data
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API
    Then each offer should have required core fields populated
    And each offer should have valid merchant information
    And each offer should have valid pricing information
    And each offer should have delivery information when available

Scenario: Test performance with anti-detection
    Given I have real scraped products available
    When I fetch offers for a random scraped product from live API with timing
    Then the request should include human-like delays between 200-800ms
    And the total request time should be reasonable (under 10 seconds)
    And anti-detection headers should be applied

Scenario: Test with different cities
    Given I have real scraped products available
    When I fetch offers for city "710000000" (Astana) using a random scraped product
    Then the request should include the correct city ID in the request body
    And the offers should reflect regional availability

Scenario: Test multiple real products with dynamic data
    Given I have real scraped products available
    When I fetch offers for each product from live API
    Then the request should succeed for valid products
    And each product should return offers that match the product ID
    And all offers should have valid basic information
    And the scraped product names should be logged for verification

@ignore
Scenario: Handle malformed JSON response gracefully
    Given I have real scraped products available
    When the API returns malformed JSON for a product
    Then I should receive an empty offers collection
    And the parsing error should be logged appropriately
    And no exceptions should be thrown

@ignore
Scenario: Handle empty JSON response
    Given I have real scraped products available
    When the API returns empty JSON for a product
    Then I should receive an empty offers collection
    And the response should be handled gracefully

Scenario: Handle JSON with missing required fields
    Given I have real scraped products available
    When the API returns JSON with missing required offer fields
    Then offers with missing required fields should be filtered out
    And only valid offers should be returned
    And invalid data should be logged

Scenario: Handle offers with invalid price data
    Given I have real scraped products available
    When the API returns offers with invalid price information
    Then offers with zero or negative prices should be filtered out
    And offers with non-numeric prices should be filtered out
    And only offers with valid positive prices should be returned

Scenario: Handle offers with invalid merchant data
    Given I have real scraped products available
    When the API returns offers with invalid merchant information
    Then offers with empty merchant names should be filtered out
    And offers with empty merchant IDs should be filtered out
    And only offers with complete merchant data should be returned

Scenario: Handle offers with invalid delivery data
    Given I have real scraped products available
    When the API returns offers with corrupted delivery information
    Then offers should handle missing delivery types gracefully
    And offers should handle invalid delivery durations gracefully
    And offers should default invalid delivery costs to reasonable values

Scenario: Handle mixed valid and invalid offer data
    Given I have real scraped products available
    When the API returns a mix of valid and invalid offers
    Then valid offers should be processed correctly
    And invalid offers should be filtered out
    And at least some valid offers should be returned if any exist
    And the filtering process should be logged
