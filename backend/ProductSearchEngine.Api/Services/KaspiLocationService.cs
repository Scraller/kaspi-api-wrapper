using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Services;

/// <summary>
/// Service for managing Kaspi.kz location data including cities and regions
/// </summary>
public class KaspiLocationService
{
    private readonly ILogger<KaspiLocationService> _logger;

    // Real data extracted from Kaspi.kz modal dialog
    private static readonly Dictionary<string, CityInfo> _kaspiCities = new()
    {
        // Major cities
        { "750000000", new CityInfo("750000000", "Алматы", "almaty", "Almaty", true) },
        { "710000000", new CityInfo("710000000", "Астана", "nur-sultan", "Astana", true) },
        { "511010000", new CityInfo("511010000", "Шымкент", "shymkent", "Shymkent", true) },
        { "471010000", new CityInfo("471010000", "Актау", "aktau", "Aktau", true) },
        { "151010000", new CityInfo("151010000", "Актобе", "aktobe", "Aktobe", true) },
        { "351010000", new CityInfo("351010000", "Караганда", "karaganda", "Karaganda", true) },
        { "231010000", new CityInfo("231010000", "Атырау", "atyrau", "Atyrau", true) },
        { "391010000", new CityInfo("391010000", "Костанай", "kostanay", "Kostanay", true) },
        { "551010000", new CityInfo("551010000", "Павлодар", "pavlodar", "Pavlodar", true) },
        { "631010000", new CityInfo("631010000", "Усть-Каменогорск", "ust-kamenogorsk", "Ust-Kamenogorsk", true) },
        { "311010000", new CityInfo("311010000", "Тараз", "taraz", "Taraz", true) },
        { "271010000", new CityInfo("271010000", "Уральск", "uralsk", "Uralsk", true) },
        { "191010000", new CityInfo("191010000", "Талдыкорган", "taldykorgan", "Taldykorgan", true) },
        { "611010000", new CityInfo("611010000", "Туркестан", "turkestan", "Turkestan", true) },
        { "451010000", new CityInfo("451010000", "Кызылорда", "kyzylorda", "Kyzylorda", true) },
        { "291010000", new CityInfo("291010000", "Петропавловск", "petropavlovsk", "Petropavlovsk", true) },
        { "631210000", new CityInfo("631210000", "Семей", "semey", "Semey", true) },

        // Major regional centers and important cities (extracted from Kaspi.kz)
        { "353220100", new CityInfo("353220100", "Абай", "abay", "Abay (Karaganda Region)", false) },
        { "515433100", new CityInfo("515433100", "Абай", "abai-t-o", "Abay (Turkestan Region)", false) },
        { "194033100", new CityInfo("194033100", "Ават", "avat-alm-obl", "Avat (Almaty Region)", false) },
        { "396430100", new CityInfo("396430100", "Айет", "ajet", "Aiyet", false) },
        { "434430100", new CityInfo("434430100", "Айтеке Би", "ayteke-bi", "Aiteke Bi", false) },
        { "314033100", new CityInfo("314033100", "Айша-Биби", "Aisha-Bibi", "Aisha-Bibi (Zhambyl Region)", false) },
        { "234230100", new CityInfo("234230100", "Аккистау", "akkistau", "Akkistau", false) },
        { "113220100", new CityInfo("113220100", "Акколь", "akkol", "Akkol", false) },
        { "116630100", new CityInfo("116630100", "Акмол", "Akmol", "Akmol (Malinovka)", false) },
        { "273620100", new CityInfo("273620100", "Аксай", "aksay", "Aksai", false) },
        { "551610000", new CityInfo("551610000", "Аксу", "aksu", "Aksu (Pavlodar Region)", false) },
        { "515230100", new CityInfo("515230100", "Аксукент", "aksukent", "Aksukent", false) },
        { "196837100", new CityInfo("196837100", "Алатау", "alatau-zhetygen", "Alatau (Zhetygen)", false) },
        { "153220100", new CityInfo("153220100", "Алга", "alga", "Alga (Aktobe Region)", false) },
        { "196433100", new CityInfo("196433100", "Алдаберген", "Aldabergen", "Aldabergen", false) },
        { "634820100", new CityInfo("634820100", "Алтай", "altai", "Altai", false) },
        { "393631100", new CityInfo("393631100", "Аманкарагай", "amankaragai", "Amankaragai", false) },
        { "433220100", new CityInfo("433220100", "Аральск", "aralsk", "Aralsk", false) },
        { "196253200", new CityInfo("196253200", "Аркабай", "arkabay", "Arkabay", false) },
        { "391610000", new CityInfo("391610000", "Аркалык", "arkalyk", "Arkalyk", false) },
        { "511610000", new CityInfo("511610000", "Арысь", "arys", "Arys", false) },
        { "314030100", new CityInfo("314030100", "Аса", "asa-zhamb-obl", "Asa (Zhambyl Region)", false) },
        { "113630100", new CityInfo("113630100", "Астраханка", "astrakhanka", "Astrakhanka", false) },
        { "514443100", new CityInfo("514443100", "Асыката", "asykata", "Asykata", false) },
        { "514459100", new CityInfo("514459100", "Атакент", "atakent-turk-obl", "Atakent (Turkestan Region)", false) },
        { "113820100", new CityInfo("113820100", "Атбасар", "atbasar", "Atbasar", false) },
        { "393630100", new CityInfo("393630100", "Аулиеколь", "auliekol", "Auliekol", false) },
        { "633420100", new CityInfo("633420100", "Аягоз", "ayagoz", "Ayagoz", false) },
        { "194043100", new CityInfo("194043100", "Байдибек бия", "baidibekbiia", "Baidibek biya", false) },
        { "431910000", new CityInfo("431910000", "Байконыр", "baikonyr", "Baikonur", false) },
        { "196847100", new CityInfo("196847100", "Байсерке", "bayserke-dmitrievka", "Bayserke (Dmitrievka)", false) },
        { "194067100", new CityInfo("194067100", "Байтерек", "baiterek-novoalekseevka", "Baiterek (Novoalekseevka)", false) },
        { "196435100", new CityInfo("196435100", "Бактыбай Жолбарысулы", "baktybay-zholbarysuly", "Baktybay Zholbarysuly", false) },
        { "194830100", new CityInfo("194830100", "Балпык Би", "Balpyk-Bi", "Balpyk Bi", false) },
        { "351610000", new CityInfo("351610000", "Балхаш", "balkhash", "Balkhash", false) },
        { "314230100", new CityInfo("314230100", "Бауржан Момышулы", "bauyrzhan-momyshuly", "Baurzhan Momyshuly (Zhambyl Region)", false) },
        { "475235100", new CityInfo("475235100", "Баутино", "bautino", "Bautino", false) },
        { "553630100", new CityInfo("553630100", "Баянаул", "bayanaul", "Bayanaul", false) },
        { "473630100", new CityInfo("473630100", "Бейнеу", "beineu", "Beineu", false) },
        { "196235100", new CityInfo("196235100", "Белбулак", "belbulak", "Belbulak", false) },
        { "634037100", new CityInfo("634037100", "Белоусовка", "belousovka", "Belousovka", false) },
        { "195233100", new CityInfo("195233100", "Береке", "bereke", "Bereke", false) },
        { "196243100", new CityInfo("196243100", "Бесагаш", "besagash", "Besagash", false) },
        { "633600000", new CityInfo("633600000", "Бескарагай", "beskaragay", "Beskaragay (Buras)", false) },
        { "595030100", new CityInfo("595030100", "Бесколь", "beskol", "Beskol", false) },
        { "116837100", new CityInfo("116837100", "Бозайгыр", "bozaigyr", "Bozaigyr", false) },
        { "395630100", new CityInfo("395630100", "Боровской", "borovskoy", "Borovskoy", false) },
        { "633830100", new CityInfo("633830100", "Бородулиха", "borodulikha", "Borodulikha", false) },
        { "354030100", new CityInfo("354030100", "Ботакара", "botakara", "Botakara", false) },
        { "593620100", new CityInfo("593620100", "Булаево", "bulayevo", "Bulayevo", false) },
        { "117035100", new CityInfo("117035100", "Бурабай", "burabay-borovoye", "Burabay (Borovoye)", false) },
        { "395439100", new CityInfo("395439100", "Владимировка", "vladimirovka", "Vladimirovka", false) },
        { "634030100", new CityInfo("634030100", "Глубокое", "glubokoe", "Glubokoe", false) },
        { "196249100", new CityInfo("196249100", "Гульдала", "guldala", "Guldala", false) },
        { "394030100", new CityInfo("394030100", "Денисовка", "denisovka", "Denisovka", false) },
        { "115420100", new CityInfo("115420100", "Державинск", "Derzhavinsk", "Derzhavinsk", false) },
        { "271000200", new CityInfo("271000200", "Деркуль", "derkyl", "Derkul", false) },
        { "235235100", new CityInfo("235235100", "Доссор", "dossor", "Dossor", false) },
        { "612010000", new CityInfo("612010000", "Кентау", "kentau", "Kentau", false) },
        { "471810000", new CityInfo("471810000", "Жанаозен", "zhanaozen", "Zhanaozen", false) },
        { "351810000", new CityInfo("351810000", "Жезказган", "zhezkazgan", "Zhezkazgan", false) },
        { "612210000", new CityInfo("612210000", "Ленгер", "lenger", "Lenger", false) },
        { "632420000", new CityInfo("632420000", "Риддер", "ridder", "Ridder", false) },
        { "392010000", new CityInfo("392010000", "Рудный", "rudnyy", "Rudny", false) },
        { "352010000", new CityInfo("352010000", "Каражал", "karazhal", "Karazhal", false) },
        { "114820100", new CityInfo("114820100", "Есиль", "esil", "Esil", false) },
        { "195620100", new CityInfo("195620100", "Жаркент", "zharkent", "Zharkent", false) },
        { "514420100", new CityInfo("514420100", "Жетысай", "zhetysai", "Zhetysai", false) },
        { "394420100", new CityInfo("394420100", "Житикара", "zhitikara", "Zhitikara", false) },
        { "634430100", new CityInfo("634430100", "Калбатау", "kalbatau", "Kalbatau", false) },
        { "154820100", new CityInfo("154820100", "Кандыагаш", "kandyagash", "Kandyagash", false) },
        { "195220100", new CityInfo("195220100", "Каскелен", "kaskelen", "Kaskelen", false) },
        { "316220100", new CityInfo("316220100", "Каратау", "karatau", "Karatau", false) },
        { "194020100", new CityInfo("194020100", "Есик", "esik", "Esik", false) },
        { "593820100", new CityInfo("593820100", "Макинск", "makinsk", "Makinsk", false) },
        { "274620100", new CityInfo("274620100", "Тайынша", "tayynsha", "Tayynsha", false) },
        { "114600000", new CityInfo("114600000", "Ерейментау", "ereimentay", "Ereimentau", false) },
        { "115630100", new CityInfo("115630100", "Зеренда", "zerenda", "Zerenda", false) },
        { "512010000", new CityInfo("512010000", "Сарыагаш", "saryagash", "Saryagash", false) },
        { "316020100", new CityInfo("316020100", "Жанатас", "janatas", "Zhanatas", false) },
        { "354820100", new CityInfo("354820100", "Каркаралинск", "karkaralinsk", "Karkaralinsk", false) },
        { "511223100", new CityInfo("511223100", "Отырар", "otyrar", "Otyrar", false) },
        { "274431100", new CityInfo("274431100", "Федоровка", "fedorovka", "Fedorovka", false) },
        { "271435100", new CityInfo("271435100", "Чапаев", "chapaev", "Chapaev", false) },
        { "394635100", new CityInfo("394635100", "Шортанды", "shortandy", "Shortandy", false) },
        { "515810000", new CityInfo("515810000", "Яссы", "yassy", "Yassy", false) }
    };

    /// <summary>
    /// Information about a city supported by Kaspi.kz
    /// </summary>
    /// <param name="Id">Kaspi internal city ID</param>
    /// <param name="Name">City name in local language</param>
    /// <param name="Slug">URL-friendly slug used by Kaspi</param>
    /// <param name="NameEn">English name of the city</param>
    /// <param name="IsMajorCity">Whether this is a major regional center</param>
    public record CityInfo(string Id, string Name, string Slug, string NameEn, bool IsMajorCity);

    /// <summary>
    /// Initialize the KaspiLocationService with logger
    /// </summary>
    /// <param name="logger">Logger for the service</param>
    public KaspiLocationService(ILogger<KaspiLocationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all supported cities from Kaspi.kz
    /// </summary>
    /// <returns>List of all supported cities</returns>
    public List<CityResponse> GetAllCities()
    {
        _logger.LogInformation("Retrieving all {CityCount} supported cities from Kaspi.kz", _kaspiCities.Count);

        return _kaspiCities.Values
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                NameEn = city.NameEn,
                Slug = city.Slug,
                IsMajorCity = city.IsMajorCity,
                IsAvailable = true, // All cities in our list are available
                KaspiUrl = $"https://kaspi.kz/shop/{city.Slug}/"
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// Get a specific city by ID
    /// </summary>
    /// <param name="cityId">Kaspi city ID</param>
    /// <returns>City information or null if not found</returns>
    public CityResponse? GetCityById(string cityId)
    {
        _logger.LogInformation("Looking up city with ID: {CityId}", cityId);

        if (!_kaspiCities.TryGetValue(cityId, out var city))
        {
            _logger.LogWarning("City with ID {CityId} not found", cityId);
            return null;
        }

        return new CityResponse
        {
            Id = city.Id,
            Name = city.Name,
            NameEn = city.NameEn,
            Slug = city.Slug,
            IsMajorCity = city.IsMajorCity,
            IsAvailable = true,
            KaspiUrl = $"https://kaspi.kz/shop/{city.Slug}/"
        };
    }

    /// <summary>
    /// Get major cities only (regional centers)
    /// </summary>
    /// <returns>List of major cities</returns>
    public List<CityResponse> GetMajorCities()
    {
        _logger.LogInformation("Retrieving major cities from Kaspi.kz");

        return _kaspiCities.Values
            .Where(city => city.IsMajorCity)
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                NameEn = city.NameEn,
                Slug = city.Slug,
                IsMajorCity = city.IsMajorCity,
                IsAvailable = true,
                KaspiUrl = $"https://kaspi.kz/shop/{city.Slug}/"
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// Search cities by name (supports partial matches)
    /// </summary>
    /// <param name="searchTerm">Search term for city name</param>
    /// <returns>List of matching cities</returns>
    public List<CityResponse> SearchCities(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetAllCities();
        }

        _logger.LogInformation("Searching cities with term: {SearchTerm}", searchTerm);

        var normalizedSearch = searchTerm.ToLowerInvariant();

        return _kaspiCities.Values
            .Where(city => 
                city.Name.ToLowerInvariant().Contains(normalizedSearch) ||
                city.NameEn.ToLowerInvariant().Contains(normalizedSearch) ||
                city.Slug.ToLowerInvariant().Contains(normalizedSearch))
            .Select(city => new CityResponse
            {
                Id = city.Id,
                Name = city.Name,
                NameEn = city.NameEn,
                Slug = city.Slug,
                IsMajorCity = city.IsMajorCity,
                IsAvailable = true,
                KaspiUrl = $"https://kaspi.kz/shop/{city.Slug}/"
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// Check if a city is supported by Kaspi.kz
    /// </summary>
    /// <param name="cityIdOrSlug">City ID or slug to check</param>
    /// <returns>True if city is supported</returns>
    public bool IsCitySupported(string cityIdOrSlug)
    {
        if (string.IsNullOrWhiteSpace(cityIdOrSlug))
            return false;

        // Check by ID first
        if (_kaspiCities.ContainsKey(cityIdOrSlug))
            return true;

        // Check by slug
        return _kaspiCities.Values.Any(c => 
            string.Equals(c.Slug, cityIdOrSlug, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get regional availability information for products/services
    /// </summary>
    /// <param name="cityId">City ID to check availability for</param>
    /// <returns>Regional availability information</returns>
    public RegionalAvailabilityResponse GetRegionalAvailability(string cityId)
    {
        _logger.LogInformation("Checking regional availability for city: {CityId}", cityId);

        var city = GetCityById(cityId);
        if (city == null)
        {
            return new RegionalAvailabilityResponse
            {
                CityId = cityId,
                CityName = "Unknown",
                IsAvailable = false,
                DeliveryOptions = new List<string>(),
                PaymentMethods = new List<string>(),
                SpecialOffers = new List<string>()
            };
        }

        // For major cities, all services are typically available
        var deliveryOptions = city.IsMajorCity 
            ? new List<string> { "Доставка", "Самовывоз", "Экспресс-доставка", "Доставка в постамат" }
            : new List<string> { "Доставка", "Самовывоз" };

        var paymentMethods = new List<string> 
        { 
            "Kaspi Red", 
            "Kaspi Gold", 
            "Рассрочка", 
            "Кредит", 
            "Наличные при получении" 
        };

        var specialOffers = city.IsMajorCity 
            ? new List<string> { "Бесплатная доставка от 15000 тг", "Кешбэк до 10%", "Специальные акции" }
            : new List<string> { "Бесплатная доставка от 15000 тг" };

        return new RegionalAvailabilityResponse
        {
            CityId = city.Id,
            CityName = city.Name,
            IsAvailable = true,
            DeliveryOptions = deliveryOptions,
            PaymentMethods = paymentMethods,
            SpecialOffers = specialOffers
        };
    }
}
