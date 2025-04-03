using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class ProductModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required(ErrorMessage = "Назва обов'язкова")]
    [Display(Name = "Назва")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Name in English is required")]
    [Display(Name = "Name (English)")]
    public string NameEn { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Категорія обов'язкова")]
    [Display(Name = "Категорія")]
    public string Category { get; set; } = string.Empty;
    
    [Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;
    
    [Display(Name = "Description (English)")]
    public string DescriptionEn { get; set; } = string.Empty;
    
    [Display(Name = "Зображення")]
    public string ImagePath { get; set; } = string.Empty;
    
    [Display(Name = "Активний")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Порядок сортування")]
    public int SortOrder { get; set; } = 0;
    
    [Display(Name = "Дата створення")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Display(Name = "Останнє оновлення")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
} 