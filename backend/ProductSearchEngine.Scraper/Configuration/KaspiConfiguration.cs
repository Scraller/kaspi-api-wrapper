namespace ProductSearchEngine.Scraper.Configuration;

/// <summary>
/// Configuration for Kaspi.kz marketplace
/// </summary>
public class KaspiConfiguration : IMarketplaceConfiguration
{
    public string MarketplaceName => "Kaspi.kz";

    public string CategoryUrlPattern => "https://kaspi.kz/shop/c/{0}/";

    public string ProductLinkSelector => ".item-card__name-link";

    public string ProductContainerSelector => ".item-card";

    public string NextPageSelector => ".pagination__el:last-child:not(.pagination__el--active)";

    public string HasNextPageScript => @"() => {
        const nextButton = document.querySelector('.pagination__el:last-child:not(.pagination__el--active)');
        return nextButton !== null;
    }";

    public string NextPageClickScript => @"() => {
        const nextButton = document.querySelector('.pagination__el:last-child:not(.pagination__el--active)');
        nextButton.click();
    }";

    public string ProductDetailLoadSelector => ".item__heading";

    public string ProductDataExtractionScript => @"() => {
        // Helper function to safely get text content
        const getText = (selector) => {
            const element = document.querySelector(selector);
            return element ? element.textContent.trim() : null;
        };
        
        // Helper function to safely get attribute value
        const getAttribute = (selector, attribute) => {
            const element = document.querySelector(selector);
            return element ? element.getAttribute(attribute) : null;
        };
        
        // Extract product ID from URL
        const getProductId = () => {
            const regex = /\/product\/([^/]+)/;
            const match = window.location.href.match(regex);
            return match ? match[1] : null;
        };
        
        // Extract product specifications
        const getSpecifications = () => {
            const specs = {};
            const rows = document.querySelectorAll('.specifications-list__spec');
            rows.forEach(row => {
                const key = row.querySelector('.specifications-list__spec-term')?.textContent.trim();
                const value = row.querySelector('.specifications-list__spec-definition')?.textContent.trim();
                if (key && value) specs[key] = value;
            });
            return specs;
        };
        
        // Extract price, removing currency symbol and spaces
        const getPriceValue = () => {
            const priceText = getText('.item__price-once');
            if (!priceText) return null;
            return parseFloat(priceText.replace(/[^0-9]/g, ''));
        };
        
        // Return structured product data
        return {
            id: getProductId(),
            name: getText('.item__heading'),
            description: getText('.item__description-text'),
            price: getPriceValue(),
            imageUrl: getAttribute('.item__gallery-preview-img', 'src'),
            brand: getText('.item__brand-text'),
            category: getText('.breadcrumbs__link:last-child'),
            inStock: !document.querySelector('.sellers-table__preorder'),
            rating: parseFloat(getText('.rating__digit') || '0'),
            reviewCount: parseInt(getText('.reviews__score-title')?.replace(/[^0-9]/g, '') || '0'),
            specifications: getSpecifications()
        };
    }";
}