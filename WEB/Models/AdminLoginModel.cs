using System.ComponentModel.DataAnnotations;

namespace WEB.Models;

public class AdminLoginModel
{
    [Required(ErrorMessage = "Логін обов'язковий")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Пароль обов'язковий")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
} 