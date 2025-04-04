using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using WEB.Localization;
using WEB.Models;

namespace WEB.Services;

public class LocalizationEditorService
{
    private readonly string _resourcesPath;
    private readonly ILogger<LocalizationEditorService> _logger;
    private readonly string[] _supportedLanguages = { "ua", "en" };
    private readonly IMemoryCache _memoryCache;
    private const string CacheKeyPrefix = "Localization_";

    public LocalizationEditorService(
        IWebHostEnvironment env, 
        ILogger<LocalizationEditorService> logger,
        IMemoryCache memoryCache)
    {
        _resourcesPath = Path.Combine(env.ContentRootPath, "Persistent", "Resources");
        _logger = logger;
        _memoryCache = memoryCache;
        
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
                    foreach (var language in _supportedLanguages)
                    {
                        var oldFilePath = Path.Combine(oldResourcesPath, $"{language}.json");
                        var newFilePath = Path.Combine(_resourcesPath, $"{language}.json");
                        
                        if (File.Exists(oldFilePath) && !File.Exists(newFilePath))
                        {
                            File.Copy(oldFilePath, newFilePath);
                            logger.LogInformation($"Файл локализации {language}.json успешно перенесен в новое расположение.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при копировании файлов локализации из старого расположения");
                }
            }
        }
    }
    
    public async Task<LocalizationViewModel> GetLocalizationResourcesAsync(string language)
    {
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
            
            // Очищаем кеш для обновленного значения
            ClearCacheForLanguage(language);
            
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
    
    private void ClearCacheForLanguage(string language)
    {
        _logger.LogInformation($"Clearing localization cache for language: {language}");
        
        try
        {
            // Создаем новый ключ кеша с временной меткой для обеспечения уникальности
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cachePrefix = $"{CacheKeyPrefix}{language.ToLower()}_{timestamp}";
            
            // Сохраняем новый ключ кеша в специальном ключе для отслеживания
            _memoryCache.Set($"LastCacheKey_{language.ToLower()}", cachePrefix, 
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            
            // Удаляем старый кеш для всех строк
            _memoryCache.Remove($"{CacheKeyPrefix}{language.ToLower()}_AllStrings");
            
            // Также очищаем кеш для других языков, чтобы избежать потенциальных конфликтов
            foreach (var otherLang in _supportedLanguages)
            {
                if (otherLang != language)
                {
                    _memoryCache.Remove($"{CacheKeyPrefix}{otherLang.ToLower()}_AllStrings");
                }
            }
            
            _logger.LogInformation($"Localization cache for language {language} cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error clearing localization cache for language {language}");
        }
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