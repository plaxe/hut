using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class ContactsModel
{
    [Display(Name = "Електронна пошта")]
    [EmailAddress(ErrorMessage = "Вкажіть коректну електронну пошту")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;
    
    [Display(Name = "Facebook")]
    public string? FacebookUrl { get; set; }
    
    [Display(Name = "Instagram")]
    public string? InstagramUrl { get; set; }
    
    [Display(Name = "WhatsApp")]
    public string? WhatsAppUrl { get; set; }
} 