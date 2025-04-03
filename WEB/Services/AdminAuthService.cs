using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WEB.Models;

namespace WEB.Services;

public class AdminAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminAuthService> _logger;
    
    public AdminAuthService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminAuthService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    public async Task<bool> ValidateLoginAsync(AdminLoginModel model)
    {
        // В реальном проекте здесь будет проверка в базе данных
        // или через API. Для примера используем хардкод или настройки
        string validUsername = _configuration["AdminAuth:Username"] ?? "admin";
        string validPassword = _configuration["AdminAuth:Password"] ?? "admin123";
        
        bool isValid = model.Username == validUsername && model.Password == validPassword;
        
        if (isValid)
        {
            await SignInAsync(model);
            _logger.LogInformation("Успішний вхід адміністратора: {Username}", model.Username);
            return true;
        }
        
        _logger.LogWarning("Невдала спроба входу для: {Username}", model.Username);
        return false;
    }
    
    private async Task SignInAsync(AdminLoginModel model)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("FullName", "Администратор сайта")
        };
        
        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3) // Сессия на 3 часа
        };
        
        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }
    
    public async Task SignOutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        
        _logger.LogInformation("Администратор вышел из системы");
    }
} 