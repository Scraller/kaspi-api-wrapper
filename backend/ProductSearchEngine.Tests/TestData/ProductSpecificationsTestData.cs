using ProductSearchEngine.Api.Models;
using ProductSearchEngine.Scraper.Kaspi;

namespace ProductSearchEngine.Tests.TestData;

/// <summary>
/// Test data generator for Product Specifications and Descriptions testing
/// Based on the discovery session findings for realistic test scenarios
/// </summary>
public static class ProductSpecificationsTestData
{
    /// <summary>
    /// Known product IDs for testing different scenarios
    /// </summary>
    public static class ProductIds
    {
        /// <summary>
        /// Product with complete specifications data (meat grinder)
        /// Based on discovery session findings
        /// </summary>
        public const string CompleteSpecifications = "129349158";
        
        /// <summary>
        /// Product with limited specifications
        /// </summary>
        public const string LimitedSpecifications = "102298404";
        
        /// <summary>
        /// Product that may not exist or have minimal data
        /// </summary>
        public const string EmptySpecifications = "999999999";
        
        /// <summary>
        /// Invalid product ID for error testing
        /// </summary>
        public const string Invalid = "invalid123";
        
        /// <summary>
        /// Product ID with special characters
        /// </summary>
        public const string SpecialCharacters = "12934%9158";
    }

    /// <summary>
    /// Known city codes for regional testing
    /// </summary>
    public static class CityCodes
    {
        public const string Almaty = "750000000";
        public const string Astana = "710000000";
        public const string Default = Almaty;
        
        public static readonly Dictionary<string, string> CityNames = new()
        {
            [Almaty] = "Алматы",
            [Astana] = "Астана"
        };
    }

    /// <summary>
    /// Expected specification groups based on discovery session
    /// </summary>
    public static class SpecificationGroups
    {
        public const string Features = "Особенности";
        public const string Characteristics = "Характеристики";
        public const string TechnicalCharacteristics = "Общие характеристики";
        public const string DimensionsAndWeight = "Габариты и вес";
        
        public static readonly string[] AllGroups = 
        {
            Features,
            Characteristics,
            TechnicalCharacteristics,
            DimensionsAndWeight
        };
    }

    /// <summary>
    /// Expected feature names based on discovery session
    /// </summary>
    public static class FeatureNames
    {
        public const string TrayMaterial = "Материал лотка";
        public const string Color = "Цвет";
        public const string NominalPower = "Номинальная мощность";
        public const string Weight = "Вес";
        public const string BodyMaterial = "Материал корпуса";
        public const string CordLength = "Длина сетевого шнура";
        public const string Performance = "Производительность";
        public const string Width = "Ширина";
        public const string Height = "Высота";
    }

    /// <summary>
    /// Expected feature values based on discovery session
    /// </summary>
    public static class FeatureValues
    {
        public const string Metal = "металл";
        public const string White = "белый";
        public const string Power2800W = "2800.0 Вт";
        public const string Weight27Kg = "2.7 кг";
        public const string CordLength1M = "1.0 м";
        public const string Performance1KgMin = "1.0 кг/мин";
        public const string Width500mm = "500.0 мм";
        public const string Height250mm = "250.0 мм";
    }

    /// <summary>
    /// Create sample product detail response with complete specifications
    /// </summary>
    public static ProductDetailResponse CreateCompleteProductWithSpecifications()
    {
        return new ProductDetailResponse
        {
            Id = ProductIds.CompleteSpecifications,
            Name = "Мясорубка электрическая Zepter ZP-987 белый",
            Slug = "mjasorubka-elektricheskaja-zepter-zp-987-belyi",
            Title = "Мясорубка Мясорубка электрическая Zepter ZP-987 белый",
            Description = "Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени, к тому же вы значительно облегчите своих денег А новые силы на кухне пригодятся вам еще для новых дел. Она быстро за считанные минуты переработает груду мясо в фарш, с специальной насадки для обработки колбасы теперь вы сможете приготовить это в домашних условиях...",
            Price = 12141,
            Currency = "KZT",
            CityCode = CityCodes.Almaty,
            CityName = CityCodes.CityNames[CityCodes.Almaty],
            Specifications = CreateCompleteSpecifications(),
            GalleryImages =
            [
                "https://kaspi.kz/img/small1.jpg",
                "https://kaspi.kz/img/medium1.jpg"
            ],
            Offers = [],
            Attributes = []
        };
    }

    /// <summary>
    /// Create sample product with limited specifications
    /// </summary>
    public static ProductDetailResponse CreateLimitedProductWithSpecifications()
    {
        return new ProductDetailResponse
        {
            Id = ProductIds.LimitedSpecifications,
            Name = "Apple iPhone 13 128GB черный",
            Slug = "apple-iphone-13-128gb-chernyi",
            Title = "Apple iPhone 13 128GB черный",
            Description = "iPhone 13 с передовыми технологиями и отличным качеством.",
            Price = 450000,
            Currency = "KZT",
            CityCode = CityCodes.Almaty,
            CityName = CityCodes.CityNames[CityCodes.Almaty],
            Specifications = CreateLimitedSpecifications(),
            GalleryImages = [],
            Offers = [],
            Attributes = []
        };
    }

    /// <summary>
    /// Create sample product with empty specifications
    /// </summary>
    public static ProductDetailResponse CreateEmptyProductWithSpecifications()
    {
        return new ProductDetailResponse
        {
            Id = ProductIds.EmptySpecifications,
            Name = "Generic Product",
            Slug = "generic-product",
            Title = "Generic Product Without Specifications",
            Description = null,
            Price = 1000,
            Currency = "KZT",
            CityCode = CityCodes.Almaty,
            CityName = CityCodes.CityNames[CityCodes.Almaty],
            Specifications = [],
            GalleryImages = [],
            Offers = [],
            Attributes = []
        };
    }

    /// <summary>
    /// Create complete specifications list based on discovery session findings
    /// </summary>
    public static List<SpecificationGroup> CreateCompleteSpecifications()
    {
        return
        [
            new SpecificationGroup
            {
                Code = "Meat grinders*Features",
                Name = SpecificationGroups.Features,
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = FeatureNames.TrayMaterial,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Metal }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = FeatureNames.BodyMaterial,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Metal }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = FeatureNames.CordLength,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.CordLength1M }
                        ]
                    }
                ]
            },
            new SpecificationGroup
            {
                Code = "Home equipment*Harakteristiki",
                Name = SpecificationGroups.Characteristics,
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = FeatureNames.Color,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.White }
                        ],
                        MultiValued = true
                    }
                ]
            },
            new SpecificationGroup
            {
                Code = "Meat grinders*Technical characteristics",
                Name = SpecificationGroups.TechnicalCharacteristics,
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = FeatureNames.NominalPower,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Power2800W }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = FeatureNames.Performance,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Performance1KgMin }
                        ]
                    }
                ]
            },
            new SpecificationGroup
            {
                Code = "Meat grinders*Dimensions and weight",
                Name = SpecificationGroups.DimensionsAndWeight,
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = FeatureNames.Width,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Width500mm }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = FeatureNames.Height,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Height250mm }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = FeatureNames.Weight,
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = FeatureValues.Weight27Kg }
                        ]
                    }
                ]
            }
        ];
    }

    /// <summary>
    /// Create limited specifications list for minimal test scenarios
    /// </summary>
    public static List<SpecificationGroup> CreateLimitedSpecifications()
    {
        return
        [
            new SpecificationGroup
            {
                Code = "Electronics*Basic",
                Name = "Основные характеристики",
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = "Тип устройства",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "Смартфон" }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = "Операционная система",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "iOS" }
                        ]
                    }
                ]
            }
        ];
    }

    /// <summary>
    /// Create sample KaspiProductDetailData for service testing
    /// </summary>
    public static KaspiProductDetailData CreateSampleKaspiProductData()
    {
        return new KaspiProductDetailData
        {
            Id = ProductIds.CompleteSpecifications,
            Title = "Мясорубка электрическая Zepter ZP-987 белый",
            Description = "Электрическая мясорубка Zepter P-987 на вашей кухне это не только большая экономия личного времени...",
            Price = 12141,
            Currency = "KZT",
            CityCode = CityCodes.Almaty,
            Specifications =
            [
                new KaspiSpecificationGroup
                {
                    Code = "Meat grinders*Features",
                    Name = SpecificationGroups.Features,
                    Position = 1,
                    Features =
                    [
                        new KaspiSpecificationFeature
                        {
                            Code = "meat grinders*tray material",
                            Name = FeatureNames.TrayMaterial,
                            Type = "ENUM",
                            Values = [FeatureValues.Metal],
                            Position = 22,
                            Visible = true,
                            MultiValued = false
                        }
                    ]
                }
            ],
            GalleryImages =
            [
                new KaspiProductImage
                {
                    Small = "https://kaspi.kz/img/small1.jpg",
                    Medium = "https://kaspi.kz/img/medium1.jpg",
                    Large = "https://kaspi.kz/img/large1.jpg",
                    Location = "location1"
                }
            ],
            ShopLink = "/shop/p/mjasorubka-elektricheskaja-zepter-zp-987-belyi-129349158/",
            Endpoint = "https://kaspi.kz/shop/rest/misc/product/mobile"
        };
    }

    /// <summary>
    /// Create multi-valued feature test data
    /// </summary>
    public static List<SpecificationGroup> CreateMultiValuedSpecifications()
    {
        return
        [
            new SpecificationGroup
            {
                Code = "Colors*Options",
                Name = "Цветовые варианты",
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = "Доступные цвета",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "красный" },
                            new SpecificationFeatureValue { Value = "синий" },
                            new SpecificationFeatureValue { Value = "зеленый" }
                        ],
                        MultiValued = true
                    }
                ]
            }
        ];
    }

    /// <summary>
    /// Create specifications with various types for testing
    /// </summary>
    public static List<SpecificationGroup> CreateVariousTypeSpecifications()
    {
        return
        [
            new SpecificationGroup
            {
                Code = "Technical*Specs",
                Name = "Технические характеристики",
                Features =
                [
                    new SpecificationFeature
                    {
                        Name = "Материал",
                        Type = "ENUM",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "пластик" }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = "Вес",
                        Type = "NUMBER",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "2.5 кг" }
                        ]
                    },
                    new SpecificationFeature
                    {
                        Name = "Описание",
                        Type = "STRING",
                        FeatureValues =
                        [
                            new SpecificationFeatureValue { Value = "Качественный товар" }
                        ]
                    }
                ]
            }
        ];
    }

    /// <summary>
    /// Validate that a specification group matches expected structure
    /// </summary>
    public static bool IsValidSpecificationGroup(SpecificationGroup group)
    {
        if (string.IsNullOrEmpty(group.Code) || string.IsNullOrEmpty(group.Name))
            return false;

        foreach (var feature in group.Features)
        {
            if (string.IsNullOrEmpty(feature.Name))
                return false;

            if (feature.FeatureValues.Any(v => string.IsNullOrEmpty(v.Value)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validate that specifications follow expected patterns from discovery
    /// </summary>
    public static bool FollowsDiscoveryPatterns(List<SpecificationGroup> specifications)
    {
        // Check if group codes follow the pattern: category*subcategory
        foreach (var group in specifications)
        {
            if (!string.IsNullOrEmpty(group.Code) && !group.Code.Contains("*"))
                return false;
        }

        return true;
    }
}
