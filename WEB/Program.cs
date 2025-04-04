using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using WEB.Localization;
using WEB.Models;
using Microsoft.AspNetCore.HttpOverrides;
using WEB.Services;
using Microsoft.Extensions.Options;
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

        // Настройка локализации должна быть перед авторизацией и после UseRouting
        app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

        // Добавляем middleware для обеспечения корректной работы локализации на EC2
        app.Use(async (context, next) =>
        {
            // Получение провайдера культуры из запроса
            var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
            var requestCulture = requestCultureFeature?.RequestCulture;

            if (requestCulture != null)
            {
                // Явно устанавливаем культуру для текущего потока
                Thread.CurrentThread.CurrentCulture = requestCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = requestCulture.UICulture;
                
                // Логируем установленную культуру для отладки
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation($"Culture set to: {Thread.CurrentThread.CurrentUICulture.Name}");
            }

            await next();
        });

        // Добавляем аутентификацию и авторизацию после локализации
        app.UseAuthentication();
        app.UseAuthorization();
        
        // OutputCache должен быть после локализации
        app.UseOutputCache();

        // Маршрут для смены языка (без кеширования)
        app.MapGet("set-language/{culture}", async (string culture, string returnUrl, HttpContext context) =>
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

            // Явно создаем новую культуру
            var requestCulture = new RequestCulture(culture);
            
            // Устанавливаем куки с большим сроком хранения и явным доменом
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                new CookieOptions { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false,
                    Path = "/",
                    Domain = context.Request.Host.Host, // Явно указываем домен
                    Secure = context.Request.IsHttps
                }
            );
            
            // Явно устанавливаем культуру для текущего запроса
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            
            // Сохраняем выбранную культуру в provider
            context.Features.Set<IRequestCultureFeature>(
                new RequestCultureFeature(requestCulture, new CookieRequestCultureProvider())
            );
            
            // Логируем для отладки
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Culture changed to {culture}. Setting cookie: {CookieRequestCultureProvider.MakeCookieValue(requestCulture)}");
            logger.LogInformation($"Current host: {context.Request.Host.Host}, IsHttps: {context.Request.IsHttps}");
            
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