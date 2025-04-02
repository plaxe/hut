using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WEB.Models;

namespace WEB.Services;

public class ProductService
{
    private readonly string _productsFilePath;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ProductService> _logger;
    private const string ProductsCacheKey = "ProductsList";
    
    public ProductService(IWebHostEnvironment env, IMemoryCache memoryCache, ILogger<ProductService> logger)
    {
        _productsFilePath = Path.Combine(env.ContentRootPath, "Data", "products.json");
        _memoryCache = memoryCache;
        _logger = logger;
        
        // Создаем директорию Data, если она не существует
        var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        
        // Создаем файл products.json, если он не существует
        if (!File.Exists(_productsFilePath))
        {
            File.WriteAllText(_productsFilePath, "[]");
        }
    }
    
    public async Task<List<ProductModel>> GetAllProductsAsync()
    {
        // Проверяем кэш
        if (_memoryCache.TryGetValue(ProductsCacheKey, out List<ProductModel> cachedProducts))
        {
            return cachedProducts;
        }
        
        try
        {
            // Если продуктов нет в кэше, загружаем их из файла
            var json = await File.ReadAllTextAsync(_productsFilePath);
            var products = JsonSerializer.Deserialize<List<ProductModel>>(json) ?? new List<ProductModel>();
            
            // Кешируем результат
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));
                
            _memoryCache.Set(ProductsCacheKey, products, cacheOptions);
            
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
        var allProducts = await GetAllProductsAsync();
        return allProducts.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
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
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(_productsFilePath, json);
        
        // Обновляем кэш
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));
            
        _memoryCache.Set(ProductsCacheKey, products, cacheOptions);
    }
} 