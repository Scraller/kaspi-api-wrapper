Feature: Merchant Information API
    As a developer using the Kaspi API wrapper
    I want to retrieve accurate merchant information from Kaspi.kz
    So that I can display correct merchant details in my application

Background:
    Given the Merchant API is configured
    And the KaspiMerchantProfileClient is set up with BACKEND.components.merchant extraction
    And the API uses direct HTML parsing instead of product search aggregation

@Phase2Discovery @MerchantProfile
Scenario: Retrieve accurate Sulpak merchant profile
    Given a valid merchant ID "Sulpak"
    When I request merchant details
    Then the merchant API response should be successful
    And the merchant should have accurate data from BACKEND.components.merchant
    And the merchant name should be "Sulpak"
    And the merchant rating should be greater than 4.8
    And the merchant review count should be greater than 15000
    And the response should include sales count as product count
    And the merchant API response time should be under 3 seconds

@Phase2Discovery @NumericMerchantId
Scenario Outline: Retrieve merchant profiles by numeric IDs
    Given a valid numeric merchant ID "<merchantId>"
    When I request merchant details
    Then the merchant API response should be successful
    And the merchant name should be "<expectedName>"
    And the merchant should have accurate sales count data
    And the merchant should have contact information including phone number
    And the response should follow ApiResponse wrapper format

    Examples:
      | merchantId | expectedName |
      | 11808018   | XAN_Comp     |
      | 2771000    | ТехноГород   |
      | 3101017    | HOMME        |
      | 6409007    | LUXTEX       |

@Phase2Discovery @SalesCount
Scenario: Verify SalesCount is always present
    Given any valid merchant ID from Phase 2 discovery
    When I request merchant details
    Then the merchant API response should be successful
    And the merchant should have a sales count greater than 0
    And the sales count should be mapped to product count in the response
    And the sales count should match the BACKEND.components.merchant data

@Phase2Discovery @ContactInformation
Scenario: Extract merchant contact information
    Given a merchant ID "11808018" known to have contact information
    When I request merchant details
    Then the merchant API response should be successful
    And the merchant should have contact information
    And the contact information should include a phone number
    And the phone number should match the format from BACKEND.components.merchant

@DataAccuracy @Phase2Correction
Scenario: Return corrected data instead of aggregated search results
    Given the merchant ID "Sulpak"
    When I request merchant details
    Then the merchant API response should be successful
    And the merchant rating should not be "4.87416666666667" from old aggregation
    And the merchant review count should not be "14919" from old aggregation
    And the merchant data should come from direct profile extraction
    And the data should be more accurate than product search aggregation

@ErrorHandling
Scenario: Handle non-existent merchant gracefully
    Given an invalid merchant ID "NonExistentMerchant123"
    When I request merchant details
    Then the response should return 404 Not Found
    And the response should follow ApiResponse wrapper format
    And the success flag should be false
    And the error message should indicate merchant not found
    And the data field should be null

@ErrorHandling
Scenario: Handle empty merchant ID
    Given an empty merchant ID ""
    When I request merchant details
    Then the response should return 400 Bad Request
    And the response should follow ApiResponse wrapper format
    And the success flag should be false
    And the error message should indicate merchant ID is required

@Performance
Scenario: Merchant API performance requirements
    Given any valid merchant ID
    When I request merchant details
    Then the merchant API response time should be under 5 seconds
    And the merchant API response should be successful
    And the API should handle concurrent requests efficiently

@HTMLParsing @BackendComponents
Scenario: Extract data from BACKEND.components.merchant object
    Given a merchant profile page with embedded BACKEND.components.merchant data
    When the HTML parsing extracts merchant information
    Then all required fields should be present: uid, name, rating, numberOfReviews, salesCount
    And optional fields should be handled correctly: logo, phone, create date
    And the JSON parsing should handle both single-line and multi-line formats
    And malformed JSON should be handled gracefully

@URLPatterns @Phase2Discovery
Scenario Outline: Construct correct merchant profile URLs
    Given a merchant ID "<merchantId>"
    When the system constructs the merchant profile URL
    Then the URL should follow the pattern "https://kaspi.kz/shop/info/merchant/<merchantId>/address-tab/"
    And the URL should be properly formatted for HTTP requests

    Examples:
      | merchantId |
      | Sulpak     |
      | 11808018   |
      | TECHNODOM  |

@CrossPhaseIntegration
Scenario: Merchant data integration with product filtering
    Given a merchant ID "Sulpak"
    When I request merchant details
    And I use the merchant for product filtering
    Then both endpoints should return consistent merchant information
    And the merchant name should match across all API responses
    And the filtering should work with the merchant profile data
