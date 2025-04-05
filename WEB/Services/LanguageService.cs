using Microsoft.Extensions.Caching.Memory;

namespace WEB.Services;

public class LanguageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LanguageService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly string[] _supportedLanguages = { "ua", "en" };
    
    // Константа для имени куки
    private const string LANGUAGE_COOKIE_NAME = "Language";
    
    // Константы
    private const string CACHE_VERSION_KEY = "LocalizationCacheVersion";
    
    public LanguageService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<LanguageService> logger,
        IMemoryCache memoryCache)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _memoryCache = memoryCache;
    }
    
    public string GetCurrentLanguage()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return "ua"; // Язык по умолчанию
        }
        
        // Проверяем наличие куки
        if (httpContext.Request.Cookies.TryGetValue(LANGUAGE_COOKIE_NAME, out string language))
        {
            // Проверяем, что значение из куки - один из поддерживаемых языков
            if (_supportedLanguages.Contains(language.ToLower()))
            {
                return language.ToLower();
            }
        }
        
        // Если куки нет или значение некорректное, возвращаем язык по умолчанию
        return "ua";
    }
    
    public void SetLanguage(string language)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in SetLanguage");
            return;
        }
        
        // Приводим к нижнему регистру
        language = language.ToLower();
        
        // Проверяем, поддерживается ли запрошенный язык
        if (!_supportedLanguages.Contains(language))
        {
            _logger.LogWarning($"Unsupported language requested: {language}");
            language = "ua"; // Используем язык по умолчанию
        }
        
        try
        {
            // Устанавливаем cookie
            httpContext.Response.Cookies.Append(
                LANGUAGE_COOKIE_NAME,
                language,
                new CookieOptions { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    HttpOnly = false
                }
            );
            
            // Очищаем кеш локализации для текущего языка
            ClearLocalizationCache(language);
            
            _logger.LogInformation($"Language set to: {language}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting language to {language}");
        }
    }
    
    public IEnumerable<string> GetSupportedLanguages()
    {
        return _supportedLanguages;
    }
    
    private void ClearLocalizationCache(string language)
    {
        // Очищаем кеш локализации для указанного языка
        if (_memoryCache != null)
        {
            // Очищаем основные ключи кеша для указанного языка
            var keysToRemove = new[] 
            {
                $"JsonStringLocalizer_{language}",
                $"JsonContent_{language}.json",
                $"LocalizationResources_{language}",
                $"LocalizedString_{language}"
            };
            
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _logger.LogInformation($"Удален ключ кеша: {key}");
            }
            
            // Обновляем версию кеша, чтобы принудительно инвалидировать его для всех компонентов
            var newVersion = DateTime.UtcNow.Ticks.ToString();
            _memoryCache.Set(CACHE_VERSION_KEY, newVersion);
            _logger.LogInformation($"Обновлена версия кеша локализации: {newVersion}");
            
            _logger.LogInformation($"Локализационный кеш для языка {language} полностью очищен");
        }
    }
} 