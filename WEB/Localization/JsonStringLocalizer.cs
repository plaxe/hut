using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Caching.Memory;

namespace WEB.Localization;

public class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly string _resourcesPath;
    private readonly string _resourceName;
    private readonly ILogger<JsonStringLocalizer<T>> _logger;
    private readonly IMemoryCache _memoryCache;
    private const string CacheKeyPrefix = "Localization_";

    public JsonStringLocalizer(string resourcesPath, string resourceName, ILogger<JsonStringLocalizer<T>> logger, IMemoryCache memoryCache)
    {
        _resourcesPath = resourcesPath;
        _resourceName = resourceName;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            var value = string.Format(format ?? name, arguments);
            return new LocalizedString(name, value);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var filePath = GetJsonPath();
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return Enumerable.Empty<LocalizedString>();
        }

        var jsonString = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(jsonString);

        var result = new List<LocalizedString>();
        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var nestedProperty in property.Value.EnumerateObject())
                {
                    result.Add(new LocalizedString($"{property.Name}.{nestedProperty.Name}", nestedProperty.Value.GetString()));
                }
            }
            else
            {
                result.Add(new LocalizedString(property.Name, property.Value.GetString()));
            }
        }

        return result;
    }

    private string GetString(string name)
    {
        var culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
        var cacheKey = $"{CacheKeyPrefix}{culture}_{name}";

        if (_memoryCache.TryGetValue(cacheKey, out string cachedValue))
        {
            return cachedValue;
        }

        var filePath = GetJsonPath();
        _logger.LogInformation($"Looking for resource file: {filePath}");
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return name;
        }

        var jsonString = File.ReadAllText(filePath);
        _logger.LogInformation($"Found JSON content: {jsonString}");
        var jsonDoc = JsonDocument.Parse(jsonString);

        var parts = name.Split('.');
        _logger.LogInformation($"Looking for key parts: {string.Join(" -> ", parts)}");
        var current = jsonDoc.RootElement;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning($"Current element is not an object when looking for part: {part}");
                return name;
            }

            if (!current.TryGetProperty(part, out var property))
            {
                _logger.LogWarning($"Could not find property: {part}");
                return name;
            }

            current = property;
            _logger.LogInformation($"Found part {part}, current value: {current}");
        }

        var result = current.GetString() ?? name;
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(24))
            .SetAbsoluteExpiration(TimeSpan.FromDays(1));
            
        _memoryCache.Set(cacheKey, result, cacheOptions);
        
        _logger.LogInformation($"Final result for key {name}: {result}");
        return result;
    }

    private string GetJsonPath()
    {
        var culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
        var culturePath = Path.Combine(_resourcesPath, $"{culture}.json");
        
        if (File.Exists(culturePath))
        {
            return culturePath;
        }

        // Fallback to base culture
        var baseCulture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
        var baseCulturePath = Path.Combine(_resourcesPath, $"{baseCulture}.json");
        
        if (File.Exists(baseCulturePath))
        {
            return baseCulturePath;
        }

        // Final fallback to default culture
        return Path.Combine(_resourcesPath, "uk.json");
    }
} 