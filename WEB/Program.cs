using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using WEB.Localization;
using WEB.Models;
using Microsoft.AspNetCore.HttpOverrides;

namespace WEB;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        
        // Add Memory Cache
        builder.Services.AddMemoryCache();
        
        // Add Output Cache
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("LocalizationPolicy", builder => 
                builder.Tag("Localization")
                      .SetVaryByHeader("Accept-Language")
                      .SetVaryByRouteValue("culture")
                      .Expire(TimeSpan.FromMinutes(10)));
        });

        // Configure JSON localization
        var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "Resources");
        
        builder.Services.AddSingleton<IStringLocalizerFactory>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            return new JsonStringLocalizerFactory(resourcesPath, loggerFactory, memoryCache);
        });
        
        builder.Services.AddScoped<IStringLocalizer<SharedResource>>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            return new JsonStringLocalizer<SharedResource>(resourcesPath, nameof(SharedResource), 
                loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), memoryCache);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // Закомментируем UseHsts() так как HTTPS будет обрабатывать Nginx
            // app.UseHsts();
        } 
        
        var supportedCultures = new[] {
            new CultureInfo("uk-UA"),
            new CultureInfo("en-US")
        };

        var localizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("uk-UA"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        };

        var provider = new RouteDataRequestCultureProvider();
        localizationOptions.RequestCultureProviders.Clear();
        localizationOptions.RequestCultureProviders.Add(provider);
        localizationOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());
        localizationOptions.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());

        // Добавляем настройку доверия прокси-заголовкам
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        });

        // Убираем перенаправление на HTTPS, так как этим будет заниматься Nginx
        // app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        // Правильный порядок middleware
        app.UseRouting();
        app.UseRequestLocalization(localizationOptions);
        app.UseOutputCache();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{culture=uk-UA}/{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}