using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;

namespace WEB.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesPath;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JsonStringLocalizerFactory(string resourcesPath, ILoggerFactory loggerFactory, IMemoryCache memoryCache = null, IHttpContextAccessor httpContextAccessor = null)
    {
        _resourcesPath = resourcesPath;
        _loggerFactory = loggerFactory;
        _memoryCache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var resourceName = resourceSource.Name;
        return new JsonStringLocalizer<object>(_resourcesPath, resourceName, 
            _loggerFactory.CreateLogger<JsonStringLocalizer<object>>(), _memoryCache, _httpContextAccessor);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer<object>(_resourcesPath, baseName, 
            _loggerFactory.CreateLogger<JsonStringLocalizer<object>>(), _memoryCache, _httpContextAccessor);
    }
} 