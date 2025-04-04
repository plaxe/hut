using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;

namespace WEB.Localization;

public class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly string _resourcesPath;
    private readonly string _resourceName;
    private readonly ILogger<JsonStringLocalizer<T>> _logger;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private const string RESOURCES_CACHE_KEY_PREFIX = "JsonStringLocalizer_";
    private const string JSON_CONTENT_CACHE_KEY_PREFIX = "JsonContent_";
    private const string STRING_CACHE_KEY_PREFIX = "LocalizedString_";

    public JsonStringLocalizer(
        string resourcesPath, 
        string resourceName, 
        ILogger<JsonStringLocalizer<T>> logger,
        IMemoryCache cache = null,
        IHttpContextAccessor httpContextAccessor = null)
    {
        _resourcesPath = resourcesPath;
        _resourceName = resourceName;
        _logger = logger;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
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
        // Получаем язык из куки через GetCurrentLanguage
        var culture = GetCurrentLanguage();
        var cacheKey = $"{RESOURCES_CACHE_KEY_PREFIX}{culture}";
        
        // Проверка кеша
        if (_cache != null && _cache.TryGetValue(cacheKey, out IEnumerable<LocalizedString> cachedStrings))
        {
            _logger.LogDebug($"Returning cached strings for culture {culture}");
            return cachedStrings;
        }
        
        var filePath = GetJsonPath();
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return Enumerable.Empty<LocalizedString>();
        }

        try
        {
            var jsonString = GetJsonContent(filePath);
            var jsonDoc = JsonDocument.Parse(jsonString);

            var result = new List<LocalizedString>();
            FlattenJsonToLocalizedStrings(jsonDoc.RootElement, "", result);
            
            // Сохраняем в кеш
            if (_cache != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromDays(1))
                    .SetSlidingExpiration(TimeSpan.FromHours(6))
                    .SetPriority(CacheItemPriority.High)
                    .SetSize(1);
                
                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogDebug($"Cached {result.Count} strings for culture {culture}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting all localized strings for culture {culture}");
            return Enumerable.Empty<LocalizedString>();
        }
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
                }
            }
        }
    }

    private string GetString(string name)
    {
        // Получаем язык из куки через GetCurrentLanguage
        var culture = GetCurrentLanguage();
        var cacheKey = $"{STRING_CACHE_KEY_PREFIX}{culture}_{name}";
        
        // Проверка кеша
        if (_cache != null && _cache.TryGetValue(cacheKey, out string cachedValue))
        {
            _logger.LogDebug($"Returning cached value for key {name} in culture {culture}");
            return cachedValue;
        }
        
        var filePath = GetJsonPath();
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return name;
        }

        try
        {
            var jsonString = GetJsonContent(filePath);
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
            
            // Сохраняем в кеш
            if (_cache != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromDays(1))
                    .SetSlidingExpiration(TimeSpan.FromHours(6))
                    .SetPriority(CacheItemPriority.High)
                    .SetSize(1);
                
                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogDebug($"Cached value for key {name} in culture {culture}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting localized string for key: {name}");
            return name;
        }
    }

    private string GetJsonContent(string filePath)
    {
        var cacheKey = $"{JSON_CONTENT_CACHE_KEY_PREFIX}{filePath}";
        
        // Проверка кеша
        if (_cache != null && _cache.TryGetValue(cacheKey, out string cachedContent))
        {
            _logger.LogDebug($"Returning cached JSON content for {filePath}");
            return cachedContent;
        }
        
        var jsonString = File.ReadAllText(filePath);
        
        // Сохраняем в кеш
        if (_cache != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(1))
                .SetSlidingExpiration(TimeSpan.FromHours(6))
                .SetPriority(CacheItemPriority.High)
                .SetSize(1);
            
            _cache.Set(cacheKey, jsonString, cacheOptions);
            _logger.LogDebug($"Cached JSON content for {filePath}");
        }
        
        return jsonString;
    }

    private string GetJsonPath()
    {
        var culture = GetCurrentLanguage();
        var culturePath = Path.Combine(_resourcesPath, $"{culture}.json");
        
        if (File.Exists(culturePath))
        {
            return culturePath;
        }

        // Final fallback to default culture
        return Path.Combine(_resourcesPath, "ua.json");
    }
    
    public void ClearCache()
    {
        if (_cache == null) return;
        
        var culture = GetCurrentLanguage();
        
        // Очищаем основной кеш для текущей культуры
        var resourcesCacheKey = $"{RESOURCES_CACHE_KEY_PREFIX}{culture}";
        _cache.Remove(resourcesCacheKey);
        
        // Очищаем кеш JSON-содержимого
        var filePath = GetJsonPath();
        var jsonContentCacheKey = $"{JSON_CONTENT_CACHE_KEY_PREFIX}{filePath}";
        _cache.Remove(jsonContentCacheKey);
        
        // Ищем и удаляем все кеши строк для текущей культуры
        // Это грубое решение, так как IMemoryCache не поддерживает поиск по префиксу
        // В реальном приложении следует использовать более продвинутый механизм
        
        _logger.LogInformation($"Очищен кеш локализации для культуры {culture}");
    }
    
    // Очищает кеш для всех культур - используется при административном обновлении
    public void ClearAllCaches()
    {
        if (_cache == null) return;
        
        // Очищаем кеш для всех поддерживаемых культур
        var cultures = new[] { "ua", "en", "uk" }; // Добавьте все поддерживаемые культуры
        
        foreach (var culture in cultures)
        {
            // Очищаем основной кеш ресурсов
            _cache.Remove($"{RESOURCES_CACHE_KEY_PREFIX}{culture}");
            
            // Очищаем кеш JSON-файла
            var jsonPath = Path.Combine(_resourcesPath, $"{culture}.json");
            _cache.Remove($"{JSON_CONTENT_CACHE_KEY_PREFIX}{jsonPath}");
            
            _logger.LogInformation($"Очищен кеш локализации для культуры {culture}");
        }
    }

    // Метод для получения текущего языка из куки
    private string GetCurrentLanguage()
    {
        var culture = "ua"; // Язык по умолчанию
        
        // Получаем HttpContext через IHttpContextAccessor
        var httpContext = _httpContextAccessor?.HttpContext;
        
        if (httpContext != null)
        {
            // Константа для имени куки
            const string LANGUAGE_COOKIE_NAME = "Language";
            
            // Проверяем наличие куки языка
            if (httpContext.Request.Cookies.TryGetValue(LANGUAGE_COOKIE_NAME, out string languageCookie))
            {
                // Поддерживаемые языки
                var supportedLanguages = new[] { "ua", "en" };
                
                // Проверяем, поддерживается ли язык
                if (supportedLanguages.Contains(languageCookie.ToLower()))
                {
                    culture = languageCookie.ToLower();
                    _logger.LogDebug($"Язык из куки: {culture}");
                }
            }
        }
        else
        {
            // Для совместимости, если HttpContext недоступен
            culture = Thread.CurrentThread.CurrentUICulture.Name.ToLower();
            
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
        }
        
        return culture;
    }
} 