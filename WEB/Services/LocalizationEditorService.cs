using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using WEB.Localization;
using WEB.Models;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace WEB.Services;

public class LocalizationEditorService
{
    private readonly string _resourcesPath;
    private readonly ILogger<LocalizationEditorService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string[] _supportedLanguages = { "ua", "en" };
    private const string LOCALIZATION_RESOURCES_CACHE_KEY_PREFIX = "LocalizationResources_";
    private const string CACHE_VERSION_KEY = "LocalizationCacheVersion";
    private readonly ILoggerFactory _loggerFactory;

    public LocalizationEditorService(
        IWebHostEnvironment env, 
        ILogger<LocalizationEditorService> logger,
        IMemoryCache cache,
        ILoggerFactory loggerFactory)
    {
        _resourcesPath = Path.Combine(env.ContentRootPath, "Persistent", "Resources");
        _logger = logger;
        _cache = cache;
        _loggerFactory = loggerFactory;
        
        // Создаем директорию Persistent/Resources, если она не существует
        if (!Directory.Exists(_resourcesPath))
        {
            Directory.CreateDirectory(_resourcesPath);
            
            // Копируем файлы из старого расположения, если они существуют
            var oldResourcesPath = Path.Combine(env.ContentRootPath, "Resources");
            if (Directory.Exists(oldResourcesPath))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(oldResourcesPath, "*.json"))
                    {
                        var fileName = Path.GetFileName(file);
                        var destPath = Path.Combine(_resourcesPath, fileName);
                        File.Copy(file, destPath, overwrite: true);
                    }
                    _logger.LogInformation("Ресурсы локализации успешно скопированы из старого расположения.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при копировании файлов локализации из старого расположения");
                }
            }
        }
    }
    
    private string GetCacheKey(string language)
    {
        return $"{LOCALIZATION_RESOURCES_CACHE_KEY_PREFIX}{language}";
    }
    
    public async Task<LocalizationViewModel> GetLocalizationResourcesAsync(string language)
    {
        var cacheKey = GetCacheKey(language);
        
        // Проверяем, есть ли данные в кеше
        if (_cache.TryGetValue(cacheKey, out LocalizationViewModel? cachedViewModel) && cachedViewModel != null)
        {
            _logger.LogInformation($"Получаем ресурсы локализации для языка {language} из кеша");
            return cachedViewModel;
        }
        
        var viewModel = new LocalizationViewModel
        {
            CurrentLanguage = language,
            AvailableLanguages = GetAvailableLanguages().ToList(),
            Categories = new List<LocalizationCategoryModel>()
        };
        
        var filePath = Path.Combine(_resourcesPath, $"{language}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return viewModel;
        }
        
        try
        {
            var jsonString = await File.ReadAllTextAsync(filePath);
            using var document = JsonDocument.Parse(jsonString);
            
            var rootElement = document.RootElement;
            viewModel.Categories = ProcessJsonElement(rootElement, string.Empty);
            
            // Сохраняем в кеш
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(1))
                .SetSlidingExpiration(TimeSpan.FromHours(6))
                .SetPriority(CacheItemPriority.High)
                .SetSize(1);
                
            _cache.Set(cacheKey, viewModel, cacheOptions);
            
            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading localization resources for language {language}");
            return viewModel;
        }
    }
    
    public async Task<bool> UpdateResourceValueAsync(string language, string key, string value)
    {
        var filePath = Path.Combine(_resourcesPath, $"{language}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Resource file not found: {filePath}");
            return false;
        }
        
        try
        {
            var jsonString = await File.ReadAllTextAsync(filePath);
            var jsonObject = JsonNode.Parse(jsonString)?.AsObject();
            
            if (jsonObject == null)
            {
                _logger.LogWarning($"Failed to parse JSON from file: {filePath}");
                return false;
            }
            
            // Разбиваем ключ по точкам
            var keyParts = key.Split('.');
            var currentNode = jsonObject;
            
            // Проходим по вложенным объектам до предпоследнего элемента
            for (int i = 0; i < keyParts.Length - 1; i++)
            {
                var part = keyParts[i];
                if (currentNode[part] == null || currentNode[part]?.GetType().Name != "JsonObject")
                {
                    _logger.LogWarning($"Invalid key structure: {key}");
                    return false;
                }
                
                currentNode = currentNode[part]!.AsObject();
            }
            
            // Устанавливаем значение последнего элемента
            var lastKeyPart = keyParts[keyParts.Length - 1];
            currentNode[lastKeyPart] = value;
            
            // Сохраняем обновленный JSON
            var options = new JsonSerializerOptions { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var updatedJson = jsonObject.ToJsonString(options);
            
            await File.WriteAllTextAsync(filePath, updatedJson);
            
            // Очищаем кеш для всех языков, так как ресурсы были обновлены администратором
            _logger.LogInformation($"Администратор обновил ресурс локализации: {language}.{key}");
            
            // Очищаем все кеши связанные с локализацией
            ClearLocalizationCaches();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating resource value for key {key} in language {language}");
            return false;
        }
    }
    
    public IEnumerable<string> GetAvailableLanguages()
    {
        var languages = new List<string>();
        
        foreach (var language in _supportedLanguages)
        {
            var filePath = Path.Combine(_resourcesPath, $"{language}.json");
            if (File.Exists(filePath))
            {
                languages.Add(language);
            }
        }
        
        return languages;
    }
    
    private List<LocalizationCategoryModel> ProcessJsonElement(JsonElement element, string parentPath)
    {
        var categories = new List<LocalizationCategoryModel>();
        
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = property.Name;
                var category = new LocalizationCategoryModel
                {
                    Key = key,
                    Path = parentPath
                };
                
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var nextPath = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}.{key}";
                    category.Categories = ProcessJsonElement(property.Value, nextPath);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var resource = new LocalizationResourceModel
                    {
                        Key = key,
                        Value = property.Value.GetString() ?? string.Empty,
                        Path = parentPath
                    };
                    
                    category.Resources.Add(resource);
                }
                
                categories.Add(category);
            }
        }
        
        return categories;
    }

    private void ClearLocalizationCaches()
    {
        try
        {
            _logger.LogInformation("Начинаем очистку кешей локализации");
            
            // 1. Очищаем кеш ресурсов локализации (используемый в админке)
            foreach (var language in _supportedLanguages)
            {
                var cacheKey = GetCacheKey(language);
                _cache.Remove(cacheKey);
                _logger.LogInformation($"Очищен кеш администратора для языка {language}");
            }
            
            // 2. Очищаем все кеши JsonStringLocalizer
            var stringLocalizer = new JsonStringLocalizer<SharedResource>(_resourcesPath, nameof(SharedResource), 
                _loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), _cache);
            stringLocalizer.ClearAllCaches();
            
            // 3. Очищаем кеши конкретных строк для всех языков
            foreach (var language in _supportedLanguages)
            {
                // Удаляем все возможные префиксы кеша
                var prefixes = new[]
                {
                    $"JsonStringLocalizer_{language}",
                    $"LocalizedString_{language}",
                    $"JsonContent_{language}.json"
                };
                
                foreach (var prefix in prefixes)
                {
                    _logger.LogInformation($"Очищен кеш для ключа {prefix}");
                    _cache.Remove(prefix);
                }
            }
            
            // 4. Сбрасываем все другие связанные кеши
            _cache.Remove("Localization_AllStrings");
            
            // 5. Обновляем версию кеша локализации (этот шаг важен для синхронизации кеша между различными экземплярами)
            var newCacheVersion = DateTime.UtcNow.Ticks.ToString();
            _cache.Set(CACHE_VERSION_KEY, newCacheVersion);
            _logger.LogInformation($"Обновлена версия кеша локализации: {newCacheVersion}");
            
            _logger.LogInformation("Все кеши локализации очищены");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке кешей локализации");
        }
    }
} 