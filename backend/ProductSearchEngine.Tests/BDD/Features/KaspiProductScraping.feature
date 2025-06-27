Feature: Kaspi Product Scraping
    As a user of the product search engine
    I want to scrape products from Kaspi marketplace
    So that I can get updated product information

  Background:
    Given the scraper service is initialized
    And the scraper is configured for testing

  Scenario: Successfully scrape a small number of products
    Given I want to scrape "smartphones" category
    When I request 10 products to be scraped
    Then the scraping should complete successfully
    And I should receive at least 10 products
    And I should receive no more than 30 products
    And all products should have valid data

  Scenario: Progress reporting during scraping
    Given I want to scrape "smartphones" category
    When I request 30 products to be scraped
    Then progress updates should be reported regularly
    And the product count should increase over time
    And the current page should increase over time

  Scenario Outline: Scrape different product categories
    Given I want to scrape "<category>" category
    When I request <product_count> products to be scraped
    Then the scraping should complete successfully
    And I should receive at least <min_expected> products

    Examples:
      | category    | product_count | min_expected |
      | smartphones |            10 |            5 |
      | notebooks   |            15 |            8 |
      | food        |             8 |            3 |

  Scenario: Handle invalid category gracefully
    Given I want to scrape "invalid_category_xyz123" category
    When I request 10 products to be scraped
    Then the scraping should complete successfully
    And I should receive 0 products
    And no errors should be thrown

  Scenario: Validate product data quality
    Given I want to scrape "smartphones" category
    When I request 20 products to be scraped
    Then all products should have non-empty IDs
    And all products should have non-empty names
    And all products should have valid URLs
    And products with prices should have valid currency information
