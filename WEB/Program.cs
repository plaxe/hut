using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using WEB.Localization;
using WEB.Models;
using Microsoft.AspNetCore.HttpOverrides;
using WEB.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;

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
        var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "Persistent", "Resources");
        
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

        builder.Services.AddLocalization(opt => { opt.ResourcesPath = "Persistent/Resources"; });

        // Добавляем HttpContext
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Настройка forwarding
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Добавляем аутентификацию для админки
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/admin/login";
            options.LogoutPath = "/admin/logout";
            options.AccessDeniedPath = "/admin/login";
            options.Cookie.Name = "AdminAuth";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(3);
            options.SlidingExpiration = true;
        });
        
        // Добавляем сервис авторизации админки
        builder.Services.AddScoped<AdminAuthService>();

        // Настройка локализации
        builder.Services.Configure<RequestLocalizationOptions>(
            options =>
            {
                var supportedCultures = new[] 
                { 
                    new CultureInfo("ua"),
                    new CultureInfo("en")
                };
                
                options.DefaultRequestCulture = new RequestCulture("ua");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                
                // Устанавливаем провайдеры локализации и их порядок
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    // Сначала проверяем cookie
                    new CookieRequestCultureProvider
                    {
                        CookieName = CookieRequestCultureProvider.DefaultCookieName
                    },
                    // Затем по Accept-Language хедеру
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
            });

        // Регистрируем сервис компиляции SCSS
        builder.Services.AddScoped<ScssCompilerService>();
        
        // Регистрируем сервис предварительной загрузки локализации
        builder.Services.AddScoped<LocalizationPreloadService>();

        // Добавляем сервис продуктов
        builder.Services.AddScoped<ProductService>();

        // Добавляем сервис редактирования локализации
        builder.Services.AddScoped<LocalizationEditorService>();

        // Добавляем сервис управления языками
        builder.Services.AddScoped<LanguageService>();

        // Добавляем сервис управления контактами
        builder.Services.AddScoped<ContactsService>();

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
            
            // Предварительно загружаем локализацию в кеш
            var localizationPreloader = scope.ServiceProvider.GetRequiredService<LocalizationPreloadService>();
            localizationPreloader.PreloadResources();
        }

        // Подключаем forwarded headers
        app.UseForwardedHeaders();

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        // Добавляем поддержку статических файлов из Persistent/Images
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "Persistent", "Images")),
            RequestPath = "/Persistent/Images"
        });

        // Правильный порядок middleware
        app.UseRouting();

        // Настройка локализации
        app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

        // Добавляем аутентификацию и авторизацию после локализации
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseOutputCache();

        // Маршрут по умолчанию (без культуры)
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            
        // Маршрут для админки
        app.MapControllerRoute(
            name: "admin",
            pattern: "admin/{action=Dashboard}/{id?}",
            defaults: new { controller = "Admin" });

        app.Run();
    }
}