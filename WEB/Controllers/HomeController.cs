using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers;

public class HomeController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<HomeController> _logger;
    private readonly ProductService _productService;
    private readonly ContactsService _contactsService;
    
    // Константа для имени куки
    private const string LANGUAGE_COOKIE_NAME = "Language";
    // Поддерживаемые языки
    private readonly string[] _supportedLanguages = { "ua", "en" };
    // Язык по умолчанию
    private const string DEFAULT_LANGUAGE = "ua";

    public HomeController(
        IStringLocalizer<SharedResource> localizer, 
        ILogger<HomeController> logger, 
        ProductService productService,
        ContactsService contactsService)
    {
        _localizer = localizer;
        _logger = logger;
        _productService = productService;
        _contactsService = contactsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Получаем язык из куки
        string currentLanguage = DEFAULT_LANGUAGE;
        
        if (HttpContext.Request.Cookies.TryGetValue(LANGUAGE_COOKIE_NAME, out string languageCookie))
        {
            // Проверяем, что значение из куки - один из поддерживаемых языков
            if (_supportedLanguages.Contains(languageCookie))
            {
                currentLanguage = languageCookie;
                _logger.LogInformation($"Язык из куки: {currentLanguage}");
            }
            else
            {
                _logger.LogWarning($"Неподдерживаемый язык в куки: {languageCookie}");
            }
        }
        else
        {
            _logger.LogInformation("Куки языка не найдена, используем язык по умолчанию");
        }
        
        // Логируем для отладки
        _logger.LogInformation($"Используемый язык: {currentLanguage}");
        
        // Устанавливаем язык в ViewData для доступа в представлении
        ViewData["CurrentLanguage"] = currentLanguage;
        
        // Устанавливаем заголовок страницы из ресурсов для выбранного языка
        ViewData["Title"] = _localizer["site.title"];
        
        // Получаем активные продукты для отображения на главной странице
        var products = await _productService.GetActiveProductsAsync();
        ViewData["Products"] = products;
        
        // Получаем контактные данные
        var contacts = await _contactsService.GetContactsAsync();
        ViewData["Contacts"] = contacts;
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}