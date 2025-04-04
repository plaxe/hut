using Microsoft.Extensions.Localization;
using WEB.Localization;
using WEB.Models;
using Microsoft.AspNetCore.HttpOverrides;
using WEB.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace WEB;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        
        // Add Memory Cache
        builder.Services.AddMemoryCache(options => 
        {
            // Настройка параметров кеша без ограничения размера
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(30); // Проверка устаревших кешей каждые 30 минут
        });
        
        // Add Output Cache
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("LocalizationPolicy", builder => 
                builder.Tag("Localization")
                      .SetVaryByHeader("Accept-Language")
                      .SetVaryByRouteValue("culture")
                      .Expire(TimeSpan.FromMinutes(10)));
                      
            // Правило, исключающее кеширование для маршрута set-language
            options.AddPolicy("NoCache", builder => builder.NoCache());
        });

        // Configure JSON localization
        var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "Persistent", "Resources");
        
        builder.Services.AddSingleton<IStringLocalizerFactory>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new JsonStringLocalizerFactory(resourcesPath, loggerFactory, memoryCache, httpContextAccessor);
        });
        
        builder.Services.AddScoped<IStringLocalizer<SharedResource>>(sp => 
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new JsonStringLocalizer<SharedResource>(resourcesPath, nameof(SharedResource), 
                loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), memoryCache, httpContextAccessor);
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

        // Регистрируем сервис компиляции SCSS
        builder.Services.AddScoped<ScssCompilerService>();
        
        // Регистрируем сервис предварительной загрузки локализации
        builder.Services.AddScoped<LocalizationPreloadService>();

        // Добавляем сервис продуктов
        builder.Services.AddScoped<ProductService>();

        // Добавляем сервис редактирования локализации
        builder.Services.AddScoped<LocalizationEditorService>();

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
            
            // Проверяем наличие файлов локализации
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Путь к ресурсам локализации: {resourcesPath}");
            
            try
            {
                if (Directory.Exists(resourcesPath))
                {
                    var files = Directory.GetFiles(resourcesPath);
                    logger.LogInformation($"Файлы ресурсов ({files.Length}): {string.Join(", ", files)}");
                }
                else
                {
                    logger.LogWarning($"Директория ресурсов не существует: {resourcesPath}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке ресурсов локализации");
            }
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

        // Добавляем аутентификацию и авторизацию 
        app.UseAuthentication();
        app.UseAuthorization();
        
        // OutputCache должен быть после локализации
        app.UseOutputCache();

        // Маршрут для смены языка (без кеширования)
        app.MapGet("set-language/{culture}", async (string culture, string returnUrl, HttpContext context, ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }

            // Конвертируем код языка, если необходимо
            if (culture == "uk")
            {
                culture = "ua";
            }
            
            // Устанавливаем cookie для языка
            var cookieOptions = new CookieOptions { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Path = "/",
                Secure = context.Request.IsHttps
            };
            
            // Устанавливаем cookie без указания домена
            context.Response.Cookies.Append(
                "Language",
                culture.ToLower(),
                cookieOptions
            );
            
            // Добавляем cookie для работы на поддоменах, если они есть
            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host) && host.Contains('.') && !host.Equals("localhost"))
            {
                try 
                {
                    var domainCookieOptions = new CookieOptions(cookieOptions)
                    {
                        Domain = "." + host  // Точка в начале для работы на поддоменах
                    };
                    
                    context.Response.Cookies.Append(
                        "Language",
                        culture.ToLower(),
                        domainCookieOptions
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Ошибка при установке cookie для домена {host}");
                }
            }
            
            // Перенаправляем на указанный URL
            return Results.Redirect(returnUrl);
        }).CacheOutput(c => c.NoCache()).WithName("SetLanguage");

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