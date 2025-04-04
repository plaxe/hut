using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using WEB.Models;
using WEB.Services;
using System.Globalization;

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