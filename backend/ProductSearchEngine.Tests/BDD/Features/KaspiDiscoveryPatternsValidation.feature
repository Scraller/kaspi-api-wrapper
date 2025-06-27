Feature: Kaspi.kz API Discovery Patterns Validation
    As an API wrapper developer
    I want to validate all discovered Kaspi.kz endpoint patterns
    So that I can build reliable API endpoints that match Kaspi's behavior

Background:
    Given the Kaspi.kz website is accessible
    And I have a valid HTTP client with appropriate headers

@Phase1Discovery @ProductDetails
Scenario: Product detail URL pattern validation
    Given I have a product with slug "apple-iphone-13-128gb-chernyi" and ID "102298404"
    When I construct a product detail URL using the pattern "/shop/p/{slug}-{id}/"
    Then the URL should follow the Phase 1 discovered pattern
    And the URL should end with a forward slash
    And the URL should contain the product ID

@Phase1Discovery @Regional
Scenario Outline: Regional parameter affects product content
    Given I have a product with slug "apple-iphone-13-128gb-chernyi" and ID "102298404"
    When I access the product with city code "<cityCode>"
    Then the page should display content for "<expectedCity>"
    And the response should be successful

    Examples:
        | cityCode  | expectedCity |
        | 750000000 | Алматы       |
        | 710000000 | Астана       |

@Phase2Discovery @MerchantFiltering
Scenario: Merchant filtering URL pattern validation
    Given I want to filter products by merchant "Sulpak" in category "smartphones"
    When I construct a merchant filter URL using the pattern ":category:Smartphones:allMerchants:Sulpak"
    Then the URL should contain the encoded filter parameter
    And the URL should follow the Phase 2 merchant filtering pattern

@Phase2Discovery @CrossCategory
Scenario Outline: Cross-category merchant access
    Given I want to access merchant "<merchantName>" products in category "<category>"
    When I apply the merchant filter to the category
    Then the request should be successful
    And the page should show products for the specified merchant and category

    Examples:
        | merchantName | category    |
        | Sulpak       | smartphones |
        | Sulpak       | computers   |
        | TECHNODOM    | smartphones |

@Phase3Discovery @SearchEnhancement
Scenario Outline: Search sort options functionality
    Given I want to search for "iPhone"
    When I apply sort option "<sortOption>"
    Then the search should return results sorted by "<sortOption>"
    And the response should be successful

    Examples:
        | sortOption |
        | relevance  |
        | price      |
        | rating     |
        | date       |

@Phase3Discovery @ComplexFiltering
Scenario: Complex filter combination with search
    Given I want to search for "iPhone" with complex filters
    And I want to filter by category "Smartphones"
    And I want to filter by merchant "Sulpak"
    And I want to filter by price range "до 100000 т"
    And I want to filter by availability zone "Magnum_ZONE1"
    When I combine all filters with search and sort by "price"
    Then the search should apply all filters correctly
    And the response should be successful

@Phase4Discovery @RegionalTesting
Scenario Outline: City code parameter across different endpoint types
    Given I want to access "<endpointType>" with city code "<cityCode>"
    When I apply the city code parameter to the endpoint
    Then the page should show content for "<expectedCity>"
    And the regional context should be correct

    Examples:
        | endpointType          | cityCode  | expectedCity |
        | product detail        | 750000000 | Алматы       |
        | category browse       | 750000000 | Алматы       |
        | product detail        | 710000000 | Астана       |
        | category browse       | 710000000 | Астана       |

@Phase4Discovery @ZoneIntegration
Scenario: Availability zone with city code combination
    Given I want to filter by availability zone "Magnum_ZONE1"
    And I want to specify city code "750000000" for Almaty
    When I combine zone filtering with city code
    Then both regional and zone filtering should be applied
    And the response should be successful

@Phase5Discovery @AutoEnhancement
Scenario: System auto-enhancement of search URLs
    Given I have a basic search URL for "iPhone" with sort "price"
    When the system processes the search request
    Then the system may auto-enhance the URL with additional parameters
    And the search results should be returned successfully

@Phase5Discovery @PatternIntegration
Scenario: Complete pattern integration across all phases
    Given I want to perform a comprehensive search combining all discovered patterns
    And I search for "MacBook"
    And I filter by category "Computers"
    And I filter by merchant "re-Store"
    And I filter by price "до 500000 т"
    And I filter by availability zone "Magnum_ZONE1"
    And I specify city code "750000000" for Almaty
    And I sort by "rating"
    When I execute the complete integrated request
    Then all filter patterns should work together
    And the response should show regional context for Almaty
    And the search results should contain "MacBook"

@Phase5Discovery @ReliabilityTesting
Scenario: Pattern reliability validation
    Given I have a reliable test URL with merchant filtering
    When I execute the same request multiple times
    Then the response should be consistent across requests
    And the success rate should be acceptable

@ErrorHandling @AllPhases
Scenario Outline: Invalid parameter handling across all phases
    Given I have an invalid "<parameterType>" value "<invalidValue>"
    When I use the invalid parameter in a request
    Then the system should handle the error gracefully
    And the response should not cause a server error

    Examples:
        | parameterType     | invalidValue      |
        | city code         | 999999999         |
        | merchant name     | NonExistentStore  |
        | availability zone | Invalid_ZONE      |
        | filter format     | invalid_filter    |

@Performance @AllPhases
Scenario Outline: Response time validation for all pattern types
    Given I have a "<patternType>" request
    When I execute the request
    Then the response time should be within acceptable limits
    And the response should be successful

    Examples:
        | patternType           |
        | product detail        |
        | category browse       |
        | merchant filtering    |
        | search enhancement    |
        | regional with filters |

@Integration @CrossPhase
Scenario: Phase dependency validation
    Given I test each discovery phase in dependency order
    When I validate Phase 1 basic product URLs
    And I validate Phase 2 merchant filtering builds on Phase 1
    And I validate Phase 3 search enhancement uses Phase 1 & 2 patterns
    And I validate Phase 4 regional parameters work with all previous phases
    And I validate Phase 5 deep testing confirms all patterns
    Then each phase should build correctly on its dependencies
    And all patterns should work in integration
