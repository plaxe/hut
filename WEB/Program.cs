using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using WEB.Localization;
using WEB.Models;
using Microsoft.AspNetCore.HttpOverrides;
using WEB.Services;
using Microsoft.Extensions.Options;

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

        builder.Services.AddLocalization(opt => { opt.ResourcesPath = "Resources"; });

        // Добавляем HttpContext
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Настройка forwarding
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Настройка локализации
        builder.Services.Configure<RequestLocalizationOptions>(
            options =>
            {
                var supportedCultures = new[] 
                { 
                    new CultureInfo("uk-UA"),
                    new CultureInfo("en-US"),
                    new CultureInfo("ru-RU")
                };
                options.DefaultRequestCulture = new RequestCulture("uk-UA");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.SetDefaultCulture("uk-UA");
            });

        // Регистрируем сервис компиляции SCSS
        builder.Services.AddScoped<ScssCompilerService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // Закомментируем UseHsts() так как HTTPS будет обрабатывать Nginx
            // app.UseHsts();
        } 
        
        // Компилируем SCSS при запуске приложения
        using (var scope = app.Services.CreateScope())
        {
            var scssCompiler = scope.ServiceProvider.GetRequiredService<ScssCompilerService>();
            scssCompiler.CompileScss();
        }

        // Подключаем forwarded headers
        app.UseForwardedHeaders();

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        // Правильный порядок middleware
        app.UseRouting();

        // Настройка локализации
        app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

        app.UseOutputCache();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{culture=uk-UA}/{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}