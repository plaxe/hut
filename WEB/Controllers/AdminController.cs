using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WEB.Models;
using WEB.Services;

namespace WEB.Controllers;

[Route("admin")]
public class AdminController(AdminAuthService authService, ILogger<AdminController> logger, ProductService productService, LocalizationEditorService localizationEditorService, ContactsService contactsService) : Controller
{
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
} 