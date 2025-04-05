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
    private const string CACHE_VERSION_KEY = "LocalizationCacheVersion";

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
        var cacheVersion = GetCacheVersion();
        var cacheKey = $"{RESOURCES_CACHE_KEY_PREFIX}{culture}_{cacheVersion}";
        
        // Проверка кеша
        if (_cache != null && _cache.TryGetValue(cacheKey, out IEnumerable<LocalizedString> cachedStrings))
        {
            _logger.LogDebug($"Returning cached strings for culture {culture} from version {cacheVersion}");
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
            var jsonString = GetJsonContent(filePath, cacheVersion);
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
                _logger.LogDebug($"Cached {result.Count} strings for culture {culture}, version {cacheVersion}");
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
        var cacheVersion = GetCacheVersion();
        var cacheKey = $"{STRING_CACHE_KEY_PREFIX}{culture}_{cacheVersion}_{name}";
        
        // Проверка кеша
        if (_cache != null && _cache.TryGetValue(cacheKey, out string cachedValue))
        {
            return cachedValue;
        }
        
        var filePath = GetJsonPath();
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Файл ресурсов не найден: {filePath}");
            return name;
        }

        try
        {
            var jsonString = GetJsonContent(filePath, cacheVersion);
            var jsonDoc = JsonDocument.Parse(jsonString);

            var parts = name.Split('.');
            var current = jsonDoc.RootElement;

            foreach (var part in parts)
            {
                if (current.ValueKind != JsonValueKind.Object)
                {
                    return name;
                }

                if (!current.TryGetProperty(part, out var property))
                {
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
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при получении локализованной строки для ключа: {name}");
            return name;
        }
    }

    private string GetJsonContent(string filePath, string cacheVersion = null)
    {
        cacheVersion = cacheVersion ?? GetCacheVersion();
        var cacheKey = $"{JSON_CONTENT_CACHE_KEY_PREFIX}{filePath}_{cacheVersion}";
        
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
            _logger.LogDebug($"Cached JSON content for {filePath}, version {cacheVersion}");
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
        
        _logger.LogInformation($"Начинаем очистку кеша локализации для языка {culture}");
        
        // Очищаем основной кеш для текущей культуры
        var resourcesCacheKey = $"{RESOURCES_CACHE_KEY_PREFIX}{culture}";
        _cache.Remove(resourcesCacheKey);
        _logger.LogInformation($"Очищен кеш: {resourcesCacheKey}");
        
        // Очищаем кеш JSON-содержимого
        var filePath = GetJsonPath();
        var jsonContentCacheKey = $"{JSON_CONTENT_CACHE_KEY_PREFIX}{filePath}";
        _cache.Remove(jsonContentCacheKey);
        _logger.LogInformation($"Очищен кеш: {jsonContentCacheKey}");
        
        // Очищаем дополнительные ключи кеша
        var additionalKeys = new[]
        {
            $"LocalizationResources_{culture}",
            $"Localization_{culture}_AllStrings"
        };
        
        foreach (var key in additionalKeys)
        {
            _cache.Remove(key);
            _logger.LogInformation($"Очищен кеш: {key}");
        }
        
        // Обновляем версию кеша
        var newVersion = DateTime.UtcNow.Ticks.ToString();
        _cache.Set(CACHE_VERSION_KEY, newVersion);
        _logger.LogInformation($"Обновлена версия кеша локализации на {newVersion}");
        
        _logger.LogInformation($"Завершена очистка кеша локализации для языка {culture}");
    }
    
    // Очищает кеш для всех культур - используется при административном обновлении
    public void ClearAllCaches()
    {
        if (_cache == null) return;
        
        // Очищаем кеш для всех поддерживаемых культур
        var cultures = new[] { "ua", "en", "uk" }; // Добавьте все поддерживаемые культуры
        
        _logger.LogInformation("Начинаем полную очистку всех кешей локализации");
        
        // Очищаем кеши по разным префиксам
        foreach (var culture in cultures)
        {
            // Основные ключи кеша
            var cacheKeys = new[] 
            { 
                $"{RESOURCES_CACHE_KEY_PREFIX}{culture}",
                $"{JSON_CONTENT_CACHE_KEY_PREFIX}{Path.Combine(_resourcesPath, $"{culture}.json")}",
                $"LocalizationResources_{culture}",
                $"Localization_{culture}_AllStrings",
                // Добавляем другие возможные ключи кеша
                $"LastCacheKey_{culture}"
            };
            
            // Очищаем все указанные ключи
            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
                _logger.LogInformation($"Очищен кеш для ключа: {key}");
            }
            
            // Попытка очистить возможные ключи для конкретных строк
            // Это не будет работать полностью, но может помочь
            for (int i = 0; i < 20; i++) // Предполагаем до 20 кешированных строк
            {
                var randomKey = $"{STRING_CACHE_KEY_PREFIX}{culture}_key{i}";
                _cache.Remove(randomKey);
            }
        }
        
        // Обновляем версию кеша
        var newVersion = DateTime.UtcNow.Ticks.ToString();
        _cache.Set(CACHE_VERSION_KEY, newVersion);
        _logger.LogInformation($"Обновлена версия кеша локализации на {newVersion}");
        
        _logger.LogInformation("Очистка кешей локализации завершена");
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

    private string GetCacheVersion()
    {
        if (_cache == null) return DateTime.UtcNow.Ticks.ToString();
        
        // Пытаемся получить версию кеша из кеша
        if (_cache.TryGetValue(CACHE_VERSION_KEY, out string version))
        {
            return version;
        }
        
        // Если версии нет - создаем новую
        version = DateTime.UtcNow.Ticks.ToString();
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromDays(7))
            .SetPriority(CacheItemPriority.High)
            .SetSize(1);
        
        _cache.Set(CACHE_VERSION_KEY, version, cacheOptions);
        _logger.LogInformation($"Создана новая версия кеша локализации: {version}");
        
        return version;
    }
} 