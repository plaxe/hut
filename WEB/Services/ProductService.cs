using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WEB.Models;

namespace WEB.Services;

public class ProductService
{
    private readonly string _productsFilePath;
    private readonly ILogger<ProductService> _logger;
    private readonly IMemoryCache _cache;
    private const string PRODUCTS_CACHE_KEY = "AllProducts";
    private const string ACTIVE_PRODUCTS_CACHE_KEY = "ActiveProducts";
    
    public ProductService(
        IWebHostEnvironment env, 
        ILogger<ProductService> logger,
        IMemoryCache cache)
    {
        _productsFilePath = Path.Combine(env.ContentRootPath, "Persistent", "Data", "products.json");
        _logger = logger;
        _cache = cache;
        
        // Создаем директорию Persistent/Data, если она не существует
        var dataDirectory = Path.Combine(env.ContentRootPath, "Persistent", "Data");
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        
        // Создаем файл products.json, если он не существует
        if (!File.Exists(_productsFilePath))
        {
            File.WriteAllText(_productsFilePath, "[]");
            
            // Копируем файл из старого расположения, если он существует
            var oldFilePath = Path.Combine(env.ContentRootPath, "Data", "products.json");
            if (File.Exists(oldFilePath))
            {
                try
                {
                    File.Copy(oldFilePath, _productsFilePath, overwrite: true);
                    _logger.LogInformation("Продукты успешно перенесены из старого расположения в новое.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при копировании файла продуктов из старого расположения");
                }
            }
        }
    }
    
    public async Task<List<ProductModel>> GetAllProductsAsync()
    {
        // Проверяем, есть ли продукты в кеше
        if (_cache.TryGetValue(PRODUCTS_CACHE_KEY, out List<ProductModel>? cachedProducts) && cachedProducts != null)
        {
            _logger.LogInformation("Получаем продукты из кеша");
            return cachedProducts;
        }
        
        try
        {
            // Загружаем продукты из файла
            var json = await File.ReadAllTextAsync(_productsFilePath);
            var products = JsonSerializer.Deserialize<List<ProductModel>>(json) ?? new List<ProductModel>();
            
            // Сохраняем в кеш на 30 минут
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetSize(1);
                
            _cache.Set(PRODUCTS_CACHE_KEY, products, cacheOptions);
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка продуктов");
            return new List<ProductModel>();
        }
    }
    
    public async Task<List<ProductModel>> GetActiveProductsAsync()
    {
        // Проверяем, есть ли активные продукты в кеше
        if (_cache.TryGetValue(ACTIVE_PRODUCTS_CACHE_KEY, out List<ProductModel>? cachedActiveProducts) && cachedActiveProducts != null)
        {
            _logger.LogInformation("Получаем активные продукты из кеша");
            return cachedActiveProducts;
        }
        
        var allProducts = await GetAllProductsAsync();
        var activeProducts = allProducts.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
        
        // Сохраняем в кеш на 30 минут
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .SetSlidingExpiration(TimeSpan.FromMinutes(10))
            .SetSize(1);
            
        _cache.Set(ACTIVE_PRODUCTS_CACHE_KEY, activeProducts, cacheOptions);
        
        return activeProducts;
    }
    
    public async Task<ProductModel?> GetProductByIdAsync(string id)
    {
        var products = await GetAllProductsAsync();
        return products.FirstOrDefault(p => p.Id == id);
    }
    
    public async Task<bool> AddProductAsync(ProductModel product)
    {
        try
        {
            var products = await GetAllProductsAsync();
            products.Add(product);
            await SaveProductsAsync(products);
            ClearCache();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении продукта");
            return false;
        }
    }
    
    public async Task<bool> UpdateProductAsync(ProductModel product)
    {
        try
        {
            var products = await GetAllProductsAsync();
            var index = products.FindIndex(p => p.Id == product.Id);
            
            if (index == -1)
            {
                return false;
            }
            
            product.UpdatedAt = DateTime.Now;
            products[index] = product;
            await SaveProductsAsync(products);
            ClearCache();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении продукта");
            return false;
        }
    }
    
    public async Task<bool> DeleteProductAsync(string id)
    {
        try
        {
            var products = await GetAllProductsAsync();
            var product = products.FirstOrDefault(p => p.Id == id);
            
            if (product == null)
            {
                return false;
            }
            
            products.Remove(product);
            await SaveProductsAsync(products);
            ClearCache();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении продукта");
            return false;
        }
    }
    
    private async Task SaveProductsAsync(List<ProductModel> products)
    {
        var json = JsonSerializer.Serialize(products, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        await File.WriteAllTextAsync(_productsFilePath, json);
    }
    
    private void ClearCache()
    {
        _logger.LogInformation("Очищаем кеш продуктов");
        _cache.Remove(PRODUCTS_CACHE_KEY);
        _cache.Remove(ACTIVE_PRODUCTS_CACHE_KEY);
    }
} 