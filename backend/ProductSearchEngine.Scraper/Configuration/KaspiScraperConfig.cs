using System.Collections.Generic;

namespace ProductSearchEngine.Scraper.Configuration
{
    public class KaspiScraperConfig
    {
        public string BaseUrl { get; set; } = "https://kaspi.kz/shop";

        // Anti-detection user agents
        public List<string> UserAgents { get; set; } = new List<string>
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36 Edg/92.0.902.55"
        };

        // City IDs for Kaspi
        public List<string> CityIds { get; set; } = new List<string>
        {
            "551010000", // Almaty
            "710000000", // Astana
            "471010000", // Karaganda
            "231010000", // Shymkent
            "191010000", // Aktobe
        };

        // Sort options
        public List<string> SortOptions { get; set; } = new List<string>
        {
            "relevance",
            "price-asc",
            "price-desc",
            "rating"
        };

        // Request settings
        public int MaxRetries { get; set; } = 3;
        public int DelayBetweenRequestsMs { get; set; } = 1500;
        public double JitterPercentage { get; set; } = 20.0;
        public double RandomPauseProbability { get; set; } = 0.1;
        public int MaxPauseSeconds { get; set; } = 3;
        public int MaxConcurrentRequests { get; set; } = 4;
    }
}