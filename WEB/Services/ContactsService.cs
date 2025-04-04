using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WEB.Models;

namespace WEB.Services;

public class ContactsService
{
    private readonly string _contactsFilePath;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ContactsService> _logger;
    private const string ContactsCacheKey = "ContactsData";
    
    public ContactsService(IWebHostEnvironment env, IMemoryCache memoryCache, ILogger<ContactsService> logger)
    {
        _contactsFilePath = Path.Combine(env.ContentRootPath, "Persistent", "Data", "contacts.json");
        _memoryCache = memoryCache;
        _logger = logger;
        
        // Создаем директорию Persistent/Data, если она не существует
        var dataDirectory = Path.Combine(env.ContentRootPath, "Persistent", "Data");
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
        
        // Создаем файл contacts.json, если он не существует
        if (!File.Exists(_contactsFilePath))
        {
            var defaultContacts = new ContactsModel
            {
                Email = "uakhutir@gmail.com",
                Phone = "0974018815",
                FacebookUrl = "#",
                InstagramUrl = "#",
                WhatsAppUrl = "#"
            };
            var json = JsonSerializer.Serialize(defaultContacts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_contactsFilePath, json);
            
            // Копируем файл из старого расположения, если он существует
            var oldFilePath = Path.Combine(env.ContentRootPath, "Data", "contacts.json");
            if (File.Exists(oldFilePath))
            {
                try
                {
                    File.Copy(oldFilePath, _contactsFilePath, overwrite: true);
                    _logger.LogInformation("Контакты успешно перенесены из старого расположения в новое.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при копировании файла контактов из старого расположения");
                }
            }
        }
    }
    
    public async Task<ContactsModel> GetContactsAsync()
    {
        // Проверяем кеш
        if (_memoryCache.TryGetValue(ContactsCacheKey, out ContactsModel cachedContacts))
        {
            return cachedContacts;
        }
        
        try
        {
            // Если контактов нет в кеше, загружаем их из файла
            var json = await File.ReadAllTextAsync(_contactsFilePath);
            var contacts = JsonSerializer.Deserialize<ContactsModel>(json) ?? new ContactsModel();
            
            // Кешируем результат
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                
            _memoryCache.Set(ContactsCacheKey, contacts, cacheOptions);
            
            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні контактних даних");
            return new ContactsModel();
        }
    }
    
    public async Task<bool> UpdateContactsAsync(ContactsModel contacts)
    {
        try
        {
            var json = JsonSerializer.Serialize(contacts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_contactsFilePath, json);
            
            // Обновляем кеш
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                
            _memoryCache.Set(ContactsCacheKey, contacts, cacheOptions);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при оновленні контактних даних");
            return false;
        }
    }
} 