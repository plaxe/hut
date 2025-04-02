using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class AdminLoginModel
{
    [Required(ErrorMessage = "Логин обязателен")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
} 