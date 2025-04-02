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
        var culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
        var cacheKey = $"{CacheKeyPrefix}{culture}_AllStrings";

        // Проверяем, есть ли коллекция строк в кеше
        if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<LocalizedString> cachedStrings))
        {
            return cachedStrings;
        }

        var filePath = GetJsonPath();
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return Enumerable.Empty<LocalizedString>();
        }

        var jsonString = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(jsonString);

        var result = new List<LocalizedString>();
        FlattenJsonToLocalizedStrings(jsonDoc.RootElement, "", result);

        // Кешируем результат
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(24))
            .SetAbsoluteExpiration(TimeSpan.FromDays(1));
            
        _memoryCache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    // Рекурсивно обходим JSON и сохраняем все строки
    private void FlattenJsonToLocalizedStrings(JsonElement element, string prefix, List<LocalizedString> strings)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    FlattenJsonToLocalizedStrings(property.Value, key, strings);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString() ?? string.Empty;
                    strings.Add(new LocalizedString(key, value));
                    
                    // Кешируем каждую строку отдельно
                    var culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
                    var cacheKey = $"{CacheKeyPrefix}{culture}_{key}";
                    
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours(24))
                        .SetAbsoluteExpiration(TimeSpan.FromDays(1));
                        
                    _memoryCache.Set(cacheKey, value, cacheOptions);
                }
            }
        }
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
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return name;
        }

        var jsonString = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(jsonString);

        var parts = name.Split('.');
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
        }

        var result = current.GetString() ?? name;
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(24))
            .SetAbsoluteExpiration(TimeSpan.FromDays(1));
            
        _memoryCache.Set(cacheKey, result, cacheOptions);
        
        return result;
    }

    private string GetJsonPath()
    {
        var culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
        
        // Если культура содержит "-", берем только первую часть
        if (culture.Contains("-"))
        {
            culture = culture.Split('-')[0];
        }
        
        // Специальная обработка для украинской локализации
        if (culture == "uk")
        {
            culture = "ua";
        }
        
        var culturePath = Path.Combine(_resourcesPath, $"{culture}.json");
        
        if (File.Exists(culturePath))
        {
            return culturePath;
        }

        // Final fallback to default culture
        return Path.Combine(_resourcesPath, "ua.json");
    }
} 