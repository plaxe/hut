using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using WEB.Models;
using WEB.Services;
using System.Globalization;
using System.Threading;

namespace WEB.Controllers;

public class HomeController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<HomeController> _logger;
    private readonly ProductService _productService;
    private readonly ContactsService _contactsService;

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
        // Логируем текущую культуру для отладки
        _logger.LogInformation($"Current culture in HomeController: {Thread.CurrentThread.CurrentCulture}");
        _logger.LogInformation($"Current UI culture in HomeController: {Thread.CurrentThread.CurrentUICulture}");
        
        // Получаем текущую культуру из HttpContext
        var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
        if (requestCultureFeature != null)
        {
            _logger.LogInformation($"Request Culture: {requestCultureFeature.RequestCulture.Culture}");
            _logger.LogInformation($"Request UI Culture: {requestCultureFeature.RequestCulture.UICulture}");
        }
        
        // Проверяем куки
        if (HttpContext.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out string cultureCookie))
        {
            _logger.LogInformation($"Culture cookie value: {cultureCookie}");
        }
        else
        {
            _logger.LogWarning("Culture cookie not found!");
        }
        
        ViewData["Title"] = _localizer["site.title"];
        
        // Получаем активные продукты для отображения на главной странице
        var products = await _productService.GetActiveProductsAsync();
        ViewData["Products"] = products;
        
        // Получаем контактные данные
        var contacts = await _contactsService.GetContactsAsync();
        ViewData["Contacts"] = contacts;
        
        // Добавляем текущую культуру в ViewData для отображения на странице
        ViewData["CurrentCulture"] = Thread.CurrentThread.CurrentUICulture.Name;
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}