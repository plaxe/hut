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
                .SetAbsoluteExpiration(TimeSpan.FromHours(6))
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                
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
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = jsonObject.ToJsonString(options);
            
            await File.WriteAllTextAsync(filePath, updatedJson);
            
            // Очищаем кеш для этого языка
            _cache.Remove(GetCacheKey(language));
            _logger.LogInformation($"Кеш локализации для языка {language} очищен после обновления");
            
            // Перезагружаем локализацию для текущей культуры
            var originalCulture = Thread.CurrentThread.CurrentUICulture;
            
            // Временно устанавливаем указанную культуру
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            
            // Локализатор загрузит ресурсы для текущей культуры
            var stringLocalizer = new JsonStringLocalizer<SharedResource>(_resourcesPath, nameof(SharedResource), 
                _loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), _cache);
            stringLocalizer.ClearCache();
            
            // Восстанавливаем исходную культуру
            Thread.CurrentThread.CurrentUICulture = originalCulture;
            
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
} 