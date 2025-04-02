using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class LocalizationEditModel
{
    [Required]
    public string Language { get; set; } = string.Empty;
    
    [Required]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
} 