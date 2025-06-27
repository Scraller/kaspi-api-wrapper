namespace ProductSearchEngine.Tests.TestData;

/// <summary>
/// Test data generator based on Phase 2 merchant discovery findings.
/// Provides real merchant data patterns discovered from BACKEND.components.merchant objects.
/// </summary>
public static class MerchantTestDataGenerator
{
    /// <summary>
    /// Merchants verified during Phase 2 discovery with accurate BACKEND.components.merchant data
    /// </summary>
    public static readonly Dictionary<string, MerchantTestData> DiscoveredMerchants = new()
    {
        ["Sulpak"] = new MerchantTestData
        {
            Uid = "Sulpak",
            Name = "Sulpak",
            Logo = "https://resources.cdn-kaspi.kz/img/cnt/mc/l/385f71ff-0ca6-43c5-9231-54438687c0a5/logo?format=merchant_logo",
            Phone = "3210",
            Create = DateTime.Parse("2016-11-08T00:21:08.115"),
            SalesCount = 50000,
            NumberOfReviews = 19046,
            Rating = 4.9m,
            Source = "Phase2Discovery",
            DiscoveryDate = new DateTime(2025, 6, 25)
        },
        
        ["11808018"] = new MerchantTestData
        {
            Uid = "11808018",
            Name = "XAN_Comp",
            Logo = null,
            Phone = "+7 (708) 028-31-30",
            Create = DateTime.Parse("2022-04-18T23:01:10.275"),
            SalesCount = 10000,
            NumberOfReviews = 206,
            Rating = 4.8m,
            Source = "Phase2Discovery",
            DiscoveryDate = new DateTime(2025, 6, 25)
        },
        
        ["2771000"] = new MerchantTestData
        {
            Uid = "2771000",
            Name = "ТехноГород",
            Logo = null,
            Phone = "+7 (xxx) xxx-xx-xx",
            Create = DateTime.Parse("2022-01-01T00:00:00.000"),
            SalesCount = 5000,
            NumberOfReviews = 105,
            Rating = 4.9m,
            Source = "Phase2Discovery",
            DiscoveryDate = new DateTime(2025, 6, 25)
        },
        
        ["3101017"] = new MerchantTestData
        {
            Uid = "3101017",
            Name = "HOMME",
            Logo = null,
            Phone = "+7 (xxx) xxx-xx-xx",
            Create = DateTime.Parse("2022-01-01T00:00:00.000"),
            SalesCount = 20000,
            NumberOfReviews = 83,
            Rating = 4.9m,
            Source = "Phase2Discovery",
            DiscoveryDate = new DateTime(2025, 6, 25)
        },
        
        ["6409007"] = new MerchantTestData
        {
            Uid = "6409007",
            Name = "LUXTEX",
            Logo = null,
            Phone = "+7 (xxx) xxx-xx-xx",
            Create = DateTime.Parse("2022-01-01T00:00:00.000"),
            SalesCount = 5000,
            NumberOfReviews = 134,
            Rating = 4.9m,
            Source = "Phase2Discovery",
            DiscoveryDate = new DateTime(2025, 6, 25)
        }
    };

    /// <summary>
    /// Gets a random verified merchant from Phase 2 discovery
    /// </summary>
    public static MerchantTestData GetRandomDiscoveredMerchant()
    {
        var merchants = DiscoveredMerchants.Values.ToArray();
        return merchants[Random.Shared.Next(merchants.Length)];
    }

    /// <summary>
    /// Gets merchants by specific criteria
    /// </summary>
    public static IEnumerable<MerchantTestData> GetMerchantsByRating(decimal minRating)
    {
        return DiscoveredMerchants.Values.Where(m => m.Rating >= minRating);
    }

    /// <summary>
    /// Gets merchants with phone numbers for contact info testing
    /// </summary>
    public static IEnumerable<MerchantTestData> GetMerchantsWithPhones()
    {
        return DiscoveredMerchants.Values.Where(m => !string.IsNullOrEmpty(m.Phone));
    }

    /// <summary>
    /// Gets merchants with numeric IDs for ID format testing
    /// </summary>
    public static IEnumerable<MerchantTestData> GetMerchantsWithNumericIds()
    {
        return DiscoveredMerchants.Values.Where(m => m.Uid.All(char.IsDigit));
    }

    /// <summary>
    /// Gets merchants with string IDs for ID format testing
    /// </summary>
    public static IEnumerable<MerchantTestData> GetMerchantsWithStringIds()
    {
        return DiscoveredMerchants.Values.Where(m => !m.Uid.All(char.IsDigit));
    }

    /// <summary>
    /// Generates test HTML content with BACKEND.components.merchant object
    /// </summary>
    public static string GenerateTestHtmlWithMerchantData(MerchantTestData merchantData)
    {
        var logoValue = merchantData.Logo != null ? $"\"{merchantData.Logo}\"" : "null";
        var phoneValue = merchantData.Phone != null ? $"\"{merchantData.Phone}\"" : "null";
        
        return $@"
<html>
<head><title>Merchant Profile: {merchantData.Name}</title></head>
<body>
    <div class=""merchant-profile"">
        <h1>{merchantData.Name}</h1>
    </div>
    <script>
        BACKEND.components.merchant = {{
            ""uid"": ""{merchantData.Uid}"",
            ""name"": ""{merchantData.Name}"",
            ""logo"": {logoValue},
            ""phone"": {phoneValue},
            ""create"": ""{merchantData.Create:yyyy-MM-ddTHH:mm:ss.fff}"",
            ""salesCount"": {merchantData.SalesCount},
            ""numberOfReviews"": {merchantData.NumberOfReviews},
            ""rating"": {merchantData.Rating}
        }}
    </script>
</body>
</html>";
    }

    /// <summary>
    /// Generates test HTML with malformed JSON for error testing
    /// </summary>
    public static string GenerateTestHtmlWithMalformedJson(string merchantId)
    {
        return $@"
<html>
<body>
    <script>
        BACKEND.components.merchant = {{""uid"":""{merchantId}"",""name"":""Test"",""invalidJson"":}}
    </script>
</body>
</html>";
    }

    /// <summary>
    /// Generates test HTML with missing BACKEND.components.merchant
    /// </summary>
    public static string GenerateTestHtmlWithoutMerchantData()
    {
        return @"
<html>
<body>
    <script>
        var someOtherData = { test: 'value' };
    </script>
</body>
</html>";
    }

    /// <summary>
    /// Gets test cases for URL pattern validation
    /// </summary>
    public static IEnumerable<object[]> GetUrlPatternTestCases()
    {
        yield return new object[] { "Sulpak", "https://kaspi.kz/shop/info/merchant/Sulpak/address-tab/" };
        yield return new object[] { "11808018", "https://kaspi.kz/shop/info/merchant/11808018/address-tab/" };
        yield return new object[] { "TECHNODOM", "https://kaspi.kz/shop/info/merchant/TECHNODOM/address-tab/" };
        yield return new object[] { "2771000", "https://kaspi.kz/shop/info/merchant/2771000/address-tab/" };
    }

    /// <summary>
    /// Gets test cases for phone number format validation
    /// </summary>
    public static IEnumerable<object[]> GetPhoneFormatTestCases()
    {
        yield return new object[] { "3210", "Sulpak short phone" };
        yield return new object[] { "+7 (708) 028-31-30", "XAN_Comp full phone" };
        yield return new object[] { "87776095552", "ИП ШИПИЛОВА numeric phone" };
        yield return new object[] { "+7 777 123-45-67", "Standard format" };
        yield return new object[] { "+7-777-123-45-67", "Dash format" };
        yield return new object[] { "8 (777) 123-45-67", "Kazakhstan format" };
    }

    /// <summary>
    /// Gets invalid merchant IDs for error testing
    /// </summary>
    public static IEnumerable<string> GetInvalidMerchantIds()
    {
        yield return "";
        yield return "   ";
        yield return "NonExistentMerchant123";
        yield return "invalid@merchant";
        yield return "merchant with spaces";
        yield return "very-long-merchant-id-that-definitely-does-not-exist-on-kaspi";
        yield return "спец символы";
        yield return "12345!@#$%";
    }

    /// <summary>
    /// Gets data accuracy comparison between old and new methods
    /// </summary>
    public static (OldMerchantData Old, MerchantTestData New) GetDataAccuracyComparison()
    {
        var oldData = new OldMerchantData
        {
            ReviewCount = 14919,
            Rating = 4.87416666666667m,
            ProductCount = 0,
            Source = "product_search_aggregation"
        };

        var newData = DiscoveredMerchants["Sulpak"];

        return (oldData, newData);
    }
}

/// <summary>
/// Test data model representing discovered merchant data from BACKEND.components.merchant
/// </summary>
public class MerchantTestData
{
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Phone { get; set; }
    public DateTime Create { get; set; }
    public int SalesCount { get; set; }
    public int NumberOfReviews { get; set; }
    public decimal Rating { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime DiscoveryDate { get; set; }
}

/// <summary>
/// Represents old aggregated merchant data for comparison testing
/// </summary>
public class OldMerchantData
{
    public int ReviewCount { get; set; }
    public decimal Rating { get; set; }
    public int ProductCount { get; set; }
    public string Source { get; set; } = string.Empty;
}
