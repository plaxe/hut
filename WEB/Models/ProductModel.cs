using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class ProductModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required(ErrorMessage = "Название обязательно")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Name in English is required")]
    [Display(Name = "Name (English)")]
    public string NameEn { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Категория обязательна")]
    [Display(Name = "Категория")]
    public string Category { get; set; } = string.Empty;
    
    [Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;
    
    [Display(Name = "Description (English)")]
    public string DescriptionEn { get; set; } = string.Empty;
    
    [Display(Name = "Изображение")]
    public string ImagePath { get; set; } = string.Empty;
    
    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Порядок сортировки")]
    public int SortOrder { get; set; } = 0;
    
    [Display(Name = "Дата создания")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Display(Name = "Последнее обновление")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
} 