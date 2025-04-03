using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;

namespace WEB.Services;

public class LanguageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LanguageService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly string[] _supportedLanguages = { "ua", "en" };
    
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
        
        var requestCultureFeature = httpContext.Features.Get<IRequestCultureFeature>();
        var culture = requestCultureFeature?.RequestCulture.Culture.Name ?? "ua";
        
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
        
        return culture.ToLower();
    }
    
    public void SetLanguage(string culture)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in SetLanguage");
            return;
        }
        
        // Проверяем, поддерживается ли запрошенный язык
        if (!_supportedLanguages.Contains(culture.ToLower()))
        {
            _logger.LogWarning($"Unsupported language requested: {culture}");
            culture = "ua"; // Используем язык по умолчанию
        }
        
        try
        {
            // Устанавливаем cookie
            httpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );
            
            // Устанавливаем культуру для текущего запроса
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            
            // Очищаем кеш локализации
            ClearLocalizationCache(culture);
            
            _logger.LogInformation($"Language set to: {culture}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting language to {culture}");
        }
    }
    
    public IEnumerable<string> GetSupportedLanguages()
    {
        return _supportedLanguages;
    }
    
    private void ClearLocalizationCache(string language)
    {
        _logger.LogInformation($"Clearing localization cache for language: {language}");
        
        try
        {
            // Создаем новый ключ кеша с временной меткой для обеспечения уникальности
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cachePrefix = $"Localization_{language.ToLower()}_{timestamp}";
            
            // Сохраняем новый ключ кеша в специальном ключе для отслеживания
            _memoryCache.Set($"LastCacheKey_{language.ToLower()}", cachePrefix, 
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            
            // Удаляем старый кеш для всех строк
            _memoryCache.Remove($"Localization_{language.ToLower()}_AllStrings");
            
            // Также очищаем кеш для других языков, чтобы избежать потенциальных конфликтов
            foreach (var otherLang in _supportedLanguages)
            {
                if (otherLang != language)
                {
                    _memoryCache.Remove($"Localization_{otherLang.ToLower()}_AllStrings");
                }
            }
            
            _logger.LogInformation($"Localization cache for language {language} cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error clearing localization cache for language {language}");
        }
    }
} 