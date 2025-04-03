using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class LanguageModel
{
    [Required]
    public string Culture { get; set; } = string.Empty;
} 