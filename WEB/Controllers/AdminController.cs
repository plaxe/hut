using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;
using Microsoft.Extensions.Caching.Memory;
using WEB.Localization;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;

namespace WEB.Controllers;

[Route("admin")]
public class AdminController(AdminAuthService authService, ILogger<AdminController> logger, ProductService productService, LocalizationEditorService localizationEditorService, ContactsService contactsService) : Controller
{
    private const string OriginalImagesPath = "wwwroot/img/original";
    private const string CACHE_VERSION_KEY = "LocalizationCacheVersion";
    private readonly DateTime originalImagesCreationTime = new(2023, 01, 01);

    [HttpGet("login")]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Products));
        }
        
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (!ModelState.IsValid)
            return View(model);

        if (await authService.ValidateLoginAsync(model))
        {
            logger.LogInformation("Адміністратор успішно увійшов до системи");
            return RedirectToAction(nameof(Products));
        }

        ModelState.AddModelError(string.Empty, "Невірне ім'я користувача або пароль");
        return View(model);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Dashboard()
    {
        return RedirectToAction(nameof(Products));
    }
    
    [HttpGet("settings")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Settings()
    {
        return RedirectToAction(nameof(Products));
    }

    // Products CRUD
    
    [HttpGet("products")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Products()
    {
        var products = await productService.GetAllProductsAsync();
        return View(products);
    }
    
    [HttpGet("products/create")]
    [Authorize(Roles = "Administrator")]
    public IActionResult CreateProduct()
    {
        ViewData["ExistingImages"] = GetExistingProductImages();
        return View(new ProductModel());
    }
    
    [HttpPost("products/create")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(ProductModel model, IFormFile? imageFile, string? existingImage)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ExistingImages"] = GetExistingProductImages();
            return View(model);
        }
        
        // Приоритет отдаем новому загруженному файлу
        if (imageFile != null && imageFile.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            
            // Создаем директорию Persistent/Images/products, если не существует
            var imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Images", "products");
            if (!Directory.Exists(imagesDirectory))
            {
                Directory.CreateDirectory(imagesDirectory);
            }
            
            var filePath = Path.Combine(imagesDirectory, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            
            model.ImagePath = $"/Persistent/Images/products/{fileName}";
        }
        // Если новый файл не загружен, но выбрано существующее изображение
        else if (!string.IsNullOrEmpty(existingImage))
        {
            model.ImagePath = existingImage;
        }
        
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;
        
        await productService.AddProductAsync(model);
        
        TempData["SuccessMessage"] = "Продукт успішно створено";
        return RedirectToAction(nameof(Products));
    }
    
    [HttpGet("products/edit/{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> EditProduct(string id)
    {
        var product = await productService.GetProductByIdAsync(id);
        
        if (product == null)
        {
            return NotFound();
        }
        
        ViewData["ExistingImages"] = GetExistingProductImages();
        return View(product);
    }
    
    [HttpPost("products/edit/{id}")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(ProductModel model, IFormFile? imageFile, string? existingImage)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ExistingImages"] = GetExistingProductImages();
            return View(model);
        }
        
        var existingProduct = await productService.GetProductByIdAsync(model.Id);
        
        if (existingProduct == null)
        {
            return NotFound();
        }
        
        // Приоритет отдаем новому загруженному файлу
        if (imageFile != null && imageFile.Length > 0)
        {
            // Не удаляем старое изображение, просто сохраняем новое
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            
            // Создаем директорию Persistent/Images/products, если не существует
            var imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Images", "products");
            if (!Directory.Exists(imagesDirectory))
            {
                Directory.CreateDirectory(imagesDirectory);
            }
            
            var filePath = Path.Combine(imagesDirectory, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            
            model.ImagePath = $"/Persistent/Images/products/{fileName}";
        }
        // Если новый файл не загружен, но выбрано существующее изображение
        else if (!string.IsNullOrEmpty(existingImage) && existingImage != existingProduct.ImagePath)
        {
            // Просто меняем путь к изображению без удаления старого
            model.ImagePath = existingImage;
        }
        else
        {
            model.ImagePath = existingProduct.ImagePath;
        }
        
        model.CreatedAt = existingProduct.CreatedAt;
        model.UpdatedAt = DateTime.Now;
        
        await productService.UpdateProductAsync(model);
        
        TempData["SuccessMessage"] = "Продукт успішно оновлено";
        return RedirectToAction(nameof(Products));
    }
    
    [HttpPost("products/delete/{id}")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var product = await productService.GetProductByIdAsync(id);
        
        if (product == null)
        {
            return NotFound();
        }
        
        // Не удаляем изображения при удалении продукта
        
        await productService.DeleteProductAsync(id);
        
        TempData["SuccessMessage"] = "Продукт успішно видалено";
        return RedirectToAction(nameof(Products));
    }
    
    [HttpPost("products/toggle/{id}")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductStatus(string id)
    {
        var product = await productService.GetProductByIdAsync(id);
        
        if (product == null)
        {
            return NotFound();
        }
        
        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.Now;
        
        await productService.UpdateProductAsync(product);
        
        TempData["SuccessMessage"] = product.IsActive 
            ? "Продукт успішно активовано" 
            : "Продукт успішно деактивовано";
            
        return RedirectToAction(nameof(Products));
    }

    // Вспомогательный метод для получения списка существующих изображений продуктов
    private List<string> GetExistingProductImages()
    {
        var images = new List<string>();
        
        // Сначала проверяем в новой директории
        var newImagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Images", "products");
        if (Directory.Exists(newImagesFolder))
        {
            foreach (var file in Directory.GetFiles(newImagesFolder))
            {
                var fileName = Path.GetFileName(file);
                images.Add($"/Persistent/Images/products/{fileName}");
            }
        }
        
        // Для совместимости проверяем также в старой директории
        var oldImagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        if (Directory.Exists(oldImagesFolder))
        {
            foreach (var file in Directory.GetFiles(oldImagesFolder))
            {
                var fileName = Path.GetFileName(file);
                var imagePath = $"/images/products/{fileName}";
                if (!images.Contains(imagePath)) // Избегаем дубликатов
                {
                    images.Add(imagePath);
                }
            }
        }
        
        return images;
    }

    // Вспомогательный метод для определения, нужно ли удалять изображение
    private bool ShouldDeleteImage(string imagePath)
    {
        // Не удаляем изображения, которые созданы до первого запуска приложения
        var originalImagesCreationTime = new DateTime(2023, 3, 9); // Дата на которую у нас уже были изображения
        
        if (string.IsNullOrEmpty(imagePath))
            return false;
            
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
        
        if (!System.IO.File.Exists(fullPath))
            return false;
            
        var fileInfo = new FileInfo(fullPath);
        
        // Считаем что если файл был создан до даты выше - это стандартный файл из дистрибутива
        return fileInfo.CreationTime > originalImagesCreationTime;
    }

    // Localization
    [HttpGet("localization")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Localization(string language = "ua")
    {
        var availableLanguages = localizationEditorService.GetAvailableLanguages();
        if (!availableLanguages.Contains(language))
        {
            language = availableLanguages.FirstOrDefault() ?? "ua";
        }
        
        var viewModel = await localizationEditorService.GetLocalizationResourcesAsync(language);
        return View(viewModel);
    }

    [HttpPost("localization/edit")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLocalization(LocalizationEditModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var success = await localizationEditorService.UpdateResourceValueAsync(model.Language, model.Key, model.Value);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Текст успішно оновлено";
            return RedirectToAction(nameof(Localization), new { language = model.Language });
        }
        
        TempData["ErrorMessage"] = "Помилка при оновленні тексту";
        return RedirectToAction(nameof(Localization), new { language = model.Language });
    }

    [HttpPost("localization/clear-cache")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearLocalizationCache()
    {
        try
        {
            // Создаем локализатор для очистки всех кешей
            var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Resources");
            var loggerFactory = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var memoryCache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            
            var stringLocalizer = new JsonStringLocalizer<SharedResource>(
                resourcesPath, 
                nameof(SharedResource),
                loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), 
                memoryCache);
                
            // Очищаем все кеши локализации
            stringLocalizer.ClearAllCaches();
            
            // Дополнительно очистим кеши с другими префиксами
            var supportedLanguages = new[] { "ua", "en" };
            foreach (var language in supportedLanguages)
            {
                // Удаляем все возможные префиксы кеша
                var prefixes = new[]
                {
                    $"JsonStringLocalizer_{language}",
                    $"LocalizedString_{language}",
                    $"JsonContent_{language}.json",
                    $"LocalizationResources_{language}"
                };
                
                foreach (var prefix in prefixes)
                {
                    memoryCache.Remove(prefix);
                    logger.LogInformation($"Очищен кеш для ключа {prefix}");
                }
            }
            
            // Принудительно очищаем общие кеши
            memoryCache.Remove("Localization_AllStrings");
            
            // Обновляем версию кеша
            var newCacheVersion = DateTime.UtcNow.Ticks.ToString();
            memoryCache.Set(CACHE_VERSION_KEY, newCacheVersion);
            logger.LogInformation($"Обновлена версия кеша локализации: {newCacheVersion}");
            
            TempData["SuccessMessage"] = "Кеш локализации успешно очищен";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при очистке кеша: {ex.Message}";
            logger.LogError(ex, "Ошибка при очистке кеша локализации");
        }
        
        // Перенаправляем на страницу локализации
        return RedirectToAction(nameof(Localization));
    }

    // Contacts Management
    [HttpGet("contacts")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Contacts()
    {
        var contacts = await contactsService.GetContactsAsync();
        return View(contacts);
    }
    
    [HttpPost("contacts")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacts(ContactsModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var success = await contactsService.UpdateContactsAsync(model);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Контактну інформацію успішно оновлено";
        }
        else
        {
            TempData["ErrorMessage"] = "Помилка при оновленні контактної інформації";
        }
        
        return RedirectToAction(nameof(Contacts));
    }

    // Временное действие для переформатирования файла products.json
    [HttpGet("reformat-products")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ReformatProducts()
    {
        try
        {
            // Получаем все продукты
            var products = await productService.GetAllProductsAsync();
            
            // Просто сохраняем их обратно - они будут сохранены с новыми настройками сериализации
            foreach (var product in products)
            {
                await productService.UpdateProductAsync(product);
            }
            
            TempData["SuccessMessage"] = "Файл продуктов успешно переформатирован";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при переформатировании файла продуктов: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Products));
    }

    // Временное действие для переформатирования файла contacts.json
    [HttpGet("reformat-contacts")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ReformatContacts()
    {
        try
        {
            // Получаем контакты
            var contacts = await contactsService.GetContactsAsync();
            
            // Сохраняем их обратно - они будут сохранены с новыми настройками сериализации
            await contactsService.UpdateContactsAsync(contacts);
            
            TempData["SuccessMessage"] = "Файл контактов успешно переформатирован";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при переформатировании файла контактов: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Contacts));
    }

    // Временное действие для переформатирования файлов локализации
    [HttpGet("reformat-localization")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ReformatLocalization()
    {
        try
        {
            var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Resources");
            var languages = new[] { "ua", "en" };
            
            foreach (var language in languages)
            {
                var filePath = Path.Combine(resourcesPath, $"{language}.json");
                if (!System.IO.File.Exists(filePath))
                {
                    continue;
                }
                
                // Считываем JSON
                var jsonString = await System.IO.File.ReadAllTextAsync(filePath);
                
                // Десериализуем и сериализуем с новыми настройками
                using var document = JsonDocument.Parse(jsonString);
                var element = document.RootElement.Clone();
                
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                var updatedJson = JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<object>(jsonString), 
                    options
                );
                
                // Перезаписываем файл
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
            }
            
            // Очищаем кеш локализации
            var loggerFactory = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var memoryCache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            
            var stringLocalizer = new JsonStringLocalizer<SharedResource>(
                resourcesPath, 
                nameof(SharedResource),
                loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), 
                memoryCache);
                
            // Очищаем все кеши локализации
            stringLocalizer.ClearAllCaches();
            
            // Явно обновляем версию кеша для надежности
            var newCacheVersion = DateTime.UtcNow.Ticks.ToString();
            memoryCache.Set(CACHE_VERSION_KEY, newCacheVersion);
            logger.LogInformation($"Обновлена версия кеша локализации: {newCacheVersion}");
            
            TempData["SuccessMessage"] = "Файлы локализации успешно переформатированы";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при переформатировании файлов локализации: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Localization));
    }

    // Backup methods
    [HttpGet("backup/export")]
    [Authorize(Roles = "Administrator")]
    public IActionResult ExportBackup()
    {
        try
        {
            // Путь к директории Persistent
            var persistentPath = Path.Combine(Directory.GetCurrentDirectory(), "Persistent");
            
            // Временный файл для zip архива
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"hut-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            
            // Создаем архив
            if (System.IO.File.Exists(tempZipPath))
            {
                System.IO.File.Delete(tempZipPath);
            }
            
            ZipFile.CreateFromDirectory(persistentPath, tempZipPath);
            
            logger.LogInformation($"Создан архив бекапа {tempZipPath}");
            
            // Возвращаем файл для скачивания
            var fileName = Path.GetFileName(tempZipPath);
            var fileBytes = System.IO.File.ReadAllBytes(tempZipPath);
            
            // Очищаем временный файл после отправки
            System.IO.File.Delete(tempZipPath);
            
            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании бекапа");
            TempData["ErrorMessage"] = $"Ошибка при создании бекапа: {ex.Message}";
            return RedirectToAction(nameof(Dashboard));
        }
    }
    
    [HttpGet("backup/import")]
    [Authorize(Roles = "Administrator")]
    public IActionResult ImportBackup()
    {
        return View();
    }
    
    [HttpPost("backup/import")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportBackup(IFormFile backupFile)
    {
        try
        {
            if (backupFile == null || backupFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Файл не выбран или пустой";
                return RedirectToAction(nameof(ImportBackup));
            }
            
            // Путь к директории Persistent
            var persistentPath = Path.Combine(Directory.GetCurrentDirectory(), "Persistent");
            
            // Создаем временную директорию для распаковки архива
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"hut-restore-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempExtractPath);
            
            // Временный файл для загруженного архива
            var tempZipPath = Path.Combine(tempExtractPath, "backup.zip");
            
            // Сохраняем загруженный файл
            using (var fileStream = new FileStream(tempZipPath, FileMode.Create))
            {
                await backupFile.CopyToAsync(fileStream);
            }
            
            // Проверяем, что это действительно zip и содержит нужные директории
            bool isValidBackup = false;
            try
            {
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    // Проверяем наличие основных директорий в архиве
                    isValidBackup = archive.Entries.Any(e => e.FullName.StartsWith("Resources/")) &&
                                    archive.Entries.Any(e => e.FullName.StartsWith("Data/"));
                }
            }
            catch
            {
                isValidBackup = false;
            }
            
            if (!isValidBackup)
            {
                // Очищаем временные файлы
                Directory.Delete(tempExtractPath, true);
                
                TempData["ErrorMessage"] = "Некорректный файл бекапа";
                return RedirectToAction(nameof(ImportBackup));
            }
            
            // Распаковываем архив во временную директорию
            var extractPath = Path.Combine(tempExtractPath, "extracted");
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);
            
            // Создаем резервную копию текущей директории Persistent
            var backupPath = Path.Combine(Path.GetTempPath(), $"hut-backup-before-restore-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(backupPath);
            
            // Копируем текущие файлы в резервную директорию
            CopyDirectory(persistentPath, backupPath);
            
            // Удаляем текущее содержимое Persistent
            try
            {
                // Удаляем содержимое поддиректорий, но не сами директории
                foreach (var dir in Directory.GetDirectories(persistentPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    foreach (var file in dirInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (var subDir in dirInfo.GetDirectories())
                    {
                        subDir.Delete(true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при очистке директории Persistent");
                // Пытаемся восстановить из резервной копии
                CopyDirectory(backupPath, persistentPath);
                
                TempData["ErrorMessage"] = $"Ошибка при импорте бекапа: {ex.Message}";
                return RedirectToAction(nameof(ImportBackup));
            }
            
            // Копируем файлы из распакованного архива в Persistent
            try
            {
                CopyDirectory(extractPath, persistentPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при копировании файлов из бекапа");
                // Пытаемся восстановить из резервной копии
                CopyDirectory(backupPath, persistentPath);
                
                TempData["ErrorMessage"] = $"Ошибка при импорте бекапа: {ex.Message}";
                return RedirectToAction(nameof(ImportBackup));
            }
            
            // Очищаем временные файлы
            try
            {
                Directory.Delete(tempExtractPath, true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось удалить временные файлы");
            }
            
            // Очищаем кеш локализации
            try
            {
                var loggerFactory = HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var memoryCache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                
                var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Persistent", "Resources");
                var stringLocalizer = new JsonStringLocalizer<SharedResource>(
                    resourcesPath, 
                    nameof(SharedResource),
                    loggerFactory.CreateLogger<JsonStringLocalizer<SharedResource>>(), 
                    memoryCache);
                    
                // Очищаем все кеши локализации
                stringLocalizer.ClearAllCaches();
                
                // Явно обновляем версию кеша для надежности
                var newCacheVersion = DateTime.UtcNow.Ticks.ToString();
                memoryCache.Set(CACHE_VERSION_KEY, newCacheVersion);
                logger.LogInformation($"Обновлена версия кеша локализации: {newCacheVersion}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при очистке кеша локализации после импорта");
            }
            
            TempData["SuccessMessage"] = "Бекап успешно импортирован";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при импорте бекапа");
            TempData["ErrorMessage"] = $"Ошибка при импорте бекапа: {ex.Message}";
            return RedirectToAction(nameof(ImportBackup));
        }
    }
    
    // Вспомогательный метод для рекурсивного копирования директорий
    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        
        // Копируем файлы
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            System.IO.File.Copy(file, destFile, true);
        }
        
        // Рекурсивно копируем поддиректории
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(directory, destDir);
        }
    }
} 