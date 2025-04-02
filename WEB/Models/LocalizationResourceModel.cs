using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class LocalizationResourceModel
{
    [Required]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    public string? Path { get; set; }
    
    public string FullKey => string.IsNullOrEmpty(Path) ? Key : $"{Path}.{Key}";
}

public class LocalizationCategoryModel
{
    public string Key { get; set; } = string.Empty;
    
    public string Path { get; set; } = string.Empty;
    
    public string FullPath => string.IsNullOrEmpty(Path) ? Key : $"{Path}.{Key}";
    
    public List<LocalizationResourceModel> Resources { get; set; } = new List<LocalizationResourceModel>();
    
    public List<LocalizationCategoryModel> Categories { get; set; } = new List<LocalizationCategoryModel>();
}

public class LocalizationViewModel
{
    public string CurrentLanguage { get; set; } = string.Empty;
    
    public List<string> AvailableLanguages { get; set; } = new List<string>();
    
    public List<LocalizationCategoryModel> Categories { get; set; } = new List<LocalizationCategoryModel>();
} 