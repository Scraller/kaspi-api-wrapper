using System.Collections.Generic;

namespace ProductSearchEngine.Scraper.Configuration
{
    /// <summary>
    /// Static configuration for Kaspi category hierarchy relationships.
    /// This can be extended to load from database or external configuration.
    /// </summary>
    public static class KaspiCategoryHierarchyConfig
    {
        /// <summary>
        /// Known parent-child relationships between categories.
        /// Key: child category slug, Value: parent category slug
        /// </summary>
        public static readonly Dictionary<string, string> KnownParentRelationships = new()
        {
            // Fashion hierarchy
            ["women%20fashion"] = "fashion",
            ["men%20fashion"] = "fashion",
            ["girl%20fashion"] = "fashion",
            ["boy%20fashion"] = "fashion",
            ["newborn%20clothing"] = "fashion",

            // Beauty hierarchy
            ["skin%20care"] = "beauty%20care",
            ["hair%20care"] = "beauty%20care",
            ["perfumes"] = "beauty%20care",
            ["body%20care"] = "beauty%20care",
            ["nail%20care"] = "beauty%20care",
            ["decorative%20cosmetics"] = "beauty%20care",
            ["beauty%20care%20equipment"] = "beauty%20care",

            // Shoes hierarchy
            ["women%20shoes"] = "shoes",
            ["men%20shoes"] = "shoes",
            ["girl%20shoes"] = "shoes",
            ["boy%20shoes"] = "shoes",
            ["shoes%20for%20babies"] = "shoes",

            // Furniture hierarchy
            ["living%20room"] = "furniture",
            ["kitchen"] = "furniture",
            ["bedroom"] = "furniture",
            ["children%20room"] = "furniture",
            ["office"] = "furniture",
            ["entrance%20hall"] = "furniture",

            // Electronics hierarchy
            ["phone%20accessories"] = "smartphones%20and%20gadgets",
            ["desktop%20computers"] = "computers",
            ["peripherals"] = "computers",
            ["audio"] = "tv_audio",

            // Home hierarchy
            ["big%20home%20appliances"] = "home%20equipment",
            ["kitchen%20appliances"] = "home%20equipment",
            ["home%20textiles"] = "home",
            ["lighting"] = "home",
            ["household%20goods"] = "home",
            ["kitchenware"] = "home",
            ["home%20interior"] = "home",
            ["vegetable%20garden%20goods"] = "home",

            // Auto hierarchy
            ["tires"] = "car%20goods",
            ["replacement%20parts"] = "car%20goods",
            ["car%20accessories"] = "car%20goods",
            ["rims"] = "car%20goods",
            ["car%20audio"] = "car%20goods",
            ["car%20chemistry%20and%20car%20care%20products"] = "car%20goods",
            ["protection%20and%20exterior%20tuning"] = "car%20goods",

            // Sports hierarchy
            ["fishing%20equipment"] = "sports%20and%20outdoors",
            ["sports%20nutrition"] = "sports%20and%20outdoors",
            ["cycling"] = "sports%20and%20outdoors",
            ["winter%20sports"] = "sports%20and%20outdoors",
            ["sports%20protection"] = "sports%20and%20outdoors",
            ["hunting%20equipment"] = "sports%20and%20outdoors",
            ["fitness"] = "sports%20and%20outdoors",
            ["camping%20and%20hiking"] = "sports%20and%20outdoors",

            // Accessories hierarchy
            ["travel%20gear"] = "fashion%20accessories",
            ["hats%20and%20scarves"] = "fashion%20accessories",
            ["fashion%20glasses%20and%20accessories"] = "fashion%20accessories",
            ["watches"] = "fashion%20accessories",
            ["accessories"] = "fashion%20accessories",
            ["clothing%20accessories"] = "fashion%20accessories",

            // Jewelry hierarchy
            ["earrings%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",
            ["bracelets%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",
            ["jewelry%20and%20bijouterie%20sets"] = "jewelry%20and%20bijouterie",
            ["rings%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",
            ["necklaces%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",
            ["pendants%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",
            ["collars%20bijouterie%20and%20jewelry"] = "jewelry%20and%20bijouterie",

            // Child goods hierarchy
            ["toys"] = "child%20goods",
            ["baby%20care"] = "child%20goods",

            // Construction hierarchy
            ["power%20tools"] = "construction%20and%20repair",
            ["hand%20tools"] = "construction%20and%20repair",
            ["plumbing"] = "construction%20and%20repair",
            ["electrical%20equipment"] = "construction%20and%20repair",
            ["decoration%20materials"] = "construction%20and%20repair",
            ["heating%20equipment"] = "construction%20and%20repair",
            ["water%20supply"] = "construction%20and%20repair",
            ["construction%20fasteners"] = "construction%20and%20repair",
            ["doors"] = "construction%20and%20repair",

            // Office and school supplies hierarchy
            ["paper%20products"] = "office%20and%20school%20supplies",
            ["office%20supplies"] = "office%20and%20school%20supplies",
            ["writing%20supplies"] = "office%20and%20school%20supplies",

            // Leisure hierarchy
            ["books"] = "leisure",
            ["party%20games"] = "leisure",
            ["hobbies%20and%20crafts"] = "leisure",

            // Pharmacy hierarchy  
            ["vitamins"] = "pharmacy",
            ["lenses%20glasses%20accessories"] = "pharmacy",

            // Pet goods hierarchy
            ["pet%20accessories"] = "pet%20goods",

            // Gifts and party supplies hierarchy
            ["gifts"] = "gifts%20and%20party%20supplies",
            ["new%20year%20decor"] = "gifts%20and%20party%20supplies",
            ["carnival%20accessories"] = "gifts%20and%20party%20supplies",
            ["gift%20wrapping%20supplies"] = "gifts%20and%20party%20supplies",
            ["holiday%20decorations"] = "gifts%20and%20party%20supplies"
        };

        /// <summary>
        /// Categories that should be excluded from the hierarchy (summary/navigation links).
        /// </summary>
        public static readonly HashSet<string> ExcludedCategoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kaspi Гид",
            "Магазин",
            "Клиентам",
            "Бизнесу",
            "ВСЕ КАТЕГОРИИ",
            "Все категории"
        };

        /// <summary>
        /// URL patterns that should be excluded from category extraction.
        /// </summary>
        public static readonly HashSet<string> ExcludedUrlPatterns = new()
        {
            "/legal/",
            "/guide/",
            "/bonus/",
            "/categories/"
        };
    }
}
