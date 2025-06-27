namespace ProductSearchEngine.Scraper.AntiDetection;

public class CityRotationManager
{
    private readonly string[] _cityIds = [
        "551010000", // Almaty
        "541000000", // Astana  
        "410000000", // Atyrau
        "150000000", // Pavlodar
        "399000000", // Aktobe
        "311000000", // Taraz
        "661000000", // Shymkent
        "710000000", // Kostanay
        "471000000", // Karaganda
        "390000000"  // Ust-Kamenogorsk
    ];

    private int _currentIndex = 0;
    private readonly Dictionary<string, int> _cityFailureCount = new();
    private readonly Dictionary<string, DateTime> _cityLastUsed = new();
    private readonly Random _random = new();
    private readonly Lock _lock = new();

    public string GetNextCityId(bool forceRotation = false)
    {
        lock (_lock)
        {
            if (forceRotation || ShouldRotateCity())
            {
                RotateToNextWorkingCity();
            }

            var cityId = _cityIds[_currentIndex];
            _cityLastUsed[cityId] = DateTime.UtcNow;
            return cityId;
        }
    }

    public void ReportCityFailure(string cityId)
    {
        lock (_lock)
        {
            _cityFailureCount[cityId] = _cityFailureCount.GetValueOrDefault(cityId) + 1;

            // If city has too many failures, rotate immediately
            if (_cityFailureCount[cityId] >= 3)
            {
                RotateToNextWorkingCity();
            }
        }
    }

    public void ReportCitySuccess(string cityId)
    {
        lock (_lock)
        {
            // Reset failure count on success
            if (_cityFailureCount.TryGetValue(cityId, out int value))
            {
                _cityFailureCount[cityId] = Math.Max(0, value - 1);
            }
        }
    }

    public string GetCurrentCityId()
    {
        lock (_lock)
        {
            return _cityIds[_currentIndex];
        }
    }

    public string[] GetAllCityIds()
    {
        // Return a copy to prevent external modification
        return (string[])_cityIds.Clone();
    }

    public Dictionary<string, object> GetRotationStats()
    {
        lock (_lock)
        {
            return new Dictionary<string, object>
            {
                ["current_city"] = _cityIds[_currentIndex],
                ["current_index"] = _currentIndex,
                ["total_cities"] = _cityIds.Length,
                ["failure_counts"] = new Dictionary<string, int>(_cityFailureCount),
                ["last_used"] = new Dictionary<string, DateTime>(_cityLastUsed)
            };
        }
    }

    private bool ShouldRotateCity()
    {
        var currentCity = _cityIds[_currentIndex];
        var lastUsed = _cityLastUsed.GetValueOrDefault(currentCity, DateTime.MinValue);

        // Rotate every 30 minutes or 2% chance per request
        return DateTime.UtcNow - lastUsed > TimeSpan.FromMinutes(30) ||
               _random.NextDouble() < 0.02;
    }

    private void RotateToNextWorkingCity()
    {
        var attemptCount = 0;
        var maxAttempts = _cityIds.Length;

        do
        {
            _currentIndex = (_currentIndex + 1) % _cityIds.Length;
            attemptCount++;

            var cityId = _cityIds[_currentIndex];
            var failureCount = _cityFailureCount.GetValueOrDefault(cityId, 0);

            // Use city if it has fewer than 5 failures
            if (failureCount < 5)
            {
                break;
            }

        } while (attemptCount < maxAttempts);

        // If all cities have high failure counts, reset all counters
        if (attemptCount >= maxAttempts)
        {
            _cityFailureCount.Clear();
        }
    }
}
