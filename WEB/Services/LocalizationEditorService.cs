using System.Text.Json;
using System.Text.Json.Nodes;
using WEB.Models;

namespace WEB.Services;

public class LocalizationEditorService
{
    private readonly string _resourcesPath;
    private readonly ILogger<LocalizationEditorService> _logger;
    private readonly string[] _supportedLanguages = { "ua", "en", "ru" };

    public LocalizationEditorService(IWebHostEnvironment env, ILogger<LocalizationEditorService> logger)
    {
        _resourcesPath = Path.Combine(env.ContentRootPath, "Resources");
        _logger = logger;
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