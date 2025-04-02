using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using WEB.Models;

namespace WEB.Services;

public class LocalizationPreloadService
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LocalizationPreloadService> _logger;
    private readonly string[] _supportedCultures = { "ua", "en", "ru" };

    public LocalizationPreloadService(
        IStringLocalizer<SharedResource> localizer,
        IMemoryCache memoryCache,
        ILogger<LocalizationPreloadService> logger)
    {
        _localizer = localizer;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public void PreloadResources()
    {
        _logger.LogInformation("Starting preloading localization resources...");
        
        var originalCulture = CultureInfo.CurrentUICulture;
        
        try
        {
            foreach (var culture in _supportedCultures)
            {
                _logger.LogInformation($"Preloading resources for culture: {culture}");
                
                // Устанавливаем текущую культуру для загрузки ресурсов
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
                
                // Получаем все строки для текущей культуры
                var allStrings = _localizer.GetAllStrings(includeParentCultures: true);
                
                foreach (var localizedString in allStrings)
                {
                    // При получении строки она уже кешируется в JsonStringLocalizer
                    var value = _localizer[localizedString.Name];
                    _logger.LogDebug($"Cached: {localizedString.Name} = {value}");
                }
                
                _logger.LogInformation($"Completed preloading resources for culture: {culture}");
            }
            
            _logger.LogInformation("Localization resources preloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while preloading localization resources");
        }
        finally
        {
            // Восстанавливаем исходную культуру
            Thread.CurrentThread.CurrentUICulture = originalCulture;
        }
    }
} 