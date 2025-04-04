using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WEB.Models;

namespace WEB.Services;

public class ContactsService
{
    private readonly string _contactsFilePath;
    private readonly ILogger<ContactsService> _logger;
    private readonly IMemoryCache _cache;
    private const string CONTACTS_CACHE_KEY = "Contacts";
    
    public ContactsService(
        IWebHostEnvironment env, 
        ILogger<ContactsService> logger,
        IMemoryCache cache)
    {
        _contactsFilePath = Path.Combine(env.ContentRootPath, "Persistent", "Data", "contacts.json");
        _logger = logger;
        _cache = cache;
        
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
        // Проверяем, есть ли контакты в кеше
        if (_cache.TryGetValue(CONTACTS_CACHE_KEY, out ContactsModel? cachedContacts) && cachedContacts != null)
        {
            _logger.LogInformation("Получаем контакты из кеша");
            return cachedContacts;
        }
        
        try
        {
            // Загружаем данные из файла
            var json = await File.ReadAllTextAsync(_contactsFilePath);
            var contacts = JsonSerializer.Deserialize<ContactsModel>(json) ?? new ContactsModel();
            
            // Сохраняем в кеш
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24)) 
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetSize(1); // Устанавливаем размер элемента
                
            _cache.Set(CONTACTS_CACHE_KEY, contacts, cacheOptions);
            
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
            
            // Очищаем кеш после обновления
            _cache.Remove(CONTACTS_CACHE_KEY);
            _logger.LogInformation("Кеш контактов очищен после обновления");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при оновленні контактних даних");
            return false;
        }
    }
} 