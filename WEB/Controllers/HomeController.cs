using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers;

public class HomeController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<HomeController> _logger;
    private readonly ProductService _productService;
    private readonly LanguageService _languageService;
    private readonly ContactsService _contactsService;

    public HomeController(
        IStringLocalizer<SharedResource> localizer, 
        ILogger<HomeController> logger, 
        ProductService productService,
        LanguageService languageService,
        ContactsService contactsService)
    {
        _localizer = localizer;
        _logger = logger;
        _productService = productService;
        _languageService = languageService;
        _contactsService = contactsService;
    }

    [HttpGet]
    [OutputCache(PolicyName = "LocalizationPolicy")]
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

    [HttpGet]
    [Route("set-culture/{culture}")]
    public IActionResult SetCulture(string culture, string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "/";
        }

        _languageService.SetLanguage(culture);
        
        return LocalRedirect(returnUrl);
    }
    
    [HttpPost]
    [Route("api/set-language")]
    public IActionResult SetLanguage([FromBody] LanguageModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Culture))
        {
            return BadRequest(new { success = false, error = "Invalid request" });
        }
        
        try
        {
            _languageService.SetLanguage(model.Culture);
            return Json(new { success = true, language = model.Culture });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error switching language to {model.Culture}");
            return StatusCode(500, new { success = false, error = "Failed to switch language" });
        }
    }
    
    [HttpGet]
    [Route("api/current-language")]
    public IActionResult GetCurrentLanguage()
    {
        var language = _languageService.GetCurrentLanguage();
        return Json(new { language });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}