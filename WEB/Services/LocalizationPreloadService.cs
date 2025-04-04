using Microsoft.Extensions.Localization;
using WEB.Models;
using WEB.Localization;
using Microsoft.AspNetCore.Http;

namespace WEB.Services;

public class LocalizationPreloadService
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<LocalizationPreloadService> _logger;
    private readonly string[] _supportedLanguages = { "ua", "en" };
    private readonly JsonStringLocalizer<SharedResource> _jsonStringLocalizer;

    public LocalizationPreloadService(
        IStringLocalizer<SharedResource> localizer,
        ILogger<LocalizationPreloadService> logger)
    {
        _localizer = localizer;
        _logger = logger;
        
        // Проверяем, можем ли мы преобразовать localizer в JsonStringLocalizer
        if (localizer is JsonStringLocalizer<SharedResource> jsonLocalizer)
        {
            _jsonStringLocalizer = jsonLocalizer;
        }
    }

    public void PreloadResources()
    {
        _logger.LogInformation("Начинаем предзагрузку ресурсов локализации...");
        
        try
        {
            // Для каждого поддерживаемого языка
            foreach (var language in _supportedLanguages)
            {
                _logger.LogInformation($"Предзагрузка ресурсов для языка: {language}");
                
                // Метод 1: Используем общий StringLocalizer (предпочтительно - пробует читать из файла)
                try
                {
                    // Явно запрашиваем файл ресурсов для каждого языка
                    var resourcePath = System.IO.Path.Combine("Persistent", "Resources", $"{language}.json");
                    if (System.IO.File.Exists(resourcePath))
                    {
                        var jsonContent = System.IO.File.ReadAllText(resourcePath);
                        _logger.LogInformation($"Загружен файл ресурсов для языка {language}, размер: {jsonContent.Length} байт");
                        
                        // Принудительно добавляем содержимое в кеш, если есть доступ к JsonStringLocalizer
                        if (_jsonStringLocalizer != null)
                        {
                            _logger.LogInformation($"Кеширование ресурсов для языка {language}");
                            // Вызов метода GetAllStrings заставит кешировать все строки
                            var allStrings = _jsonStringLocalizer.GetAllStrings(includeParentCultures: false);
                            _logger.LogInformation($"Закешировано {allStrings.Count()} строк для языка {language}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Файл ресурсов не найден для языка: {language}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при предзагрузке ресурсов для языка {language}");
                }
            }
            
            _logger.LogInformation("Ресурсы локализации успешно предзагружены");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при предзагрузке ресурсов локализации");
        }
    }
} 