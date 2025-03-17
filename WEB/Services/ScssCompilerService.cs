using LibSassHost;
using System.Text;

namespace WEB.Services
{
    public class ScssCompilerService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ScssCompilerService> _logger;

        public ScssCompilerService(IWebHostEnvironment webHostEnvironment, ILogger<ScssCompilerService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public void CompileScss()
        {
            try
            {
                string wwwrootPath = _webHostEnvironment.WebRootPath;
                string scssPath = Path.Combine(wwwrootPath, "scss");
                string cssPath = Path.Combine(wwwrootPath, "css");

                // Проверяем наличие папки css, если нет - создаем
                if (!Directory.Exists(cssPath))
                {
                    Directory.CreateDirectory(cssPath);
                }

                // Находим главный файл SCSS (main.scss)
                string mainScssPath = Path.Combine(scssPath, "main.scss");
                if (!File.Exists(mainScssPath))
                {
                    _logger.LogError("Файл main.scss не найден в папке {ScssPath}", scssPath);
                    return;
                }

                // Читаем содержимое main.scss
                string scssContent = File.ReadAllText(mainScssPath, Encoding.UTF8);

                // Компилируем SCSS в CSS
                CompilationResult result = SassCompiler.Compile(scssContent, new CompilationOptions
                {
                    OutputStyle = OutputStyle.Compressed,
                    IncludePaths = new List<string> { scssPath }
                });

                // Сохраняем результат в файл site.css
                string cssFilePath = Path.Combine(cssPath, "site.css");
                File.WriteAllText(cssFilePath, result.CompiledContent, Encoding.UTF8);

                _logger.LogInformation("SCSS успешно скомпилирован в CSS: {CssFilePath}", cssFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при компиляции SCSS: {Message}", ex.Message);
            }
        }
    }
} 