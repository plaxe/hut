using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;

namespace WEB.Controllers;

public class HomeController(ILogger<HomeController> logger, IOutputCacheStore outputCacheStore) : Controller
{
    [OutputCache(VaryByHeaderNames = ["Accept-Language"])]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> SetLanguage(string culture)
    {
        if (culture != "uk-UA" && culture != "en-US")
        {
            culture = "uk-UA";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            }
        );

        await outputCacheStore.EvictByTagAsync("Localization", default);

        return RedirectToAction("Index", new { culture });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}