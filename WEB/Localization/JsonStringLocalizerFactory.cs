using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Caching.Memory;

namespace WEB.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesPath;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _memoryCache;

    public JsonStringLocalizerFactory(string resourcesPath, ILoggerFactory loggerFactory, IMemoryCache memoryCache)
    {
        _resourcesPath = resourcesPath;
        _loggerFactory = loggerFactory;
        _memoryCache = memoryCache;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var resourceName = resourceSource.Name;
        return new JsonStringLocalizer<object>(_resourcesPath, resourceName, _loggerFactory.CreateLogger<JsonStringLocalizer<object>>(), _memoryCache);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer<object>(_resourcesPath, baseName, _loggerFactory.CreateLogger<JsonStringLocalizer<object>>(), _memoryCache);
    }
} 