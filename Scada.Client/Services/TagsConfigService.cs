using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Scada.Client.Models;

namespace Scada.Client.Services;

/// <summary>
/// Сервис для загрузки конфигурации тегов из файла tags.json.
/// Приоритет загрузки:
/// 1. %AppData%\Scada.Client\tags.json (пользовательская копия для редактирования)
/// 2. Каталог приложения\tags.json (встроенный файл)
/// </summary>
public class TagsConfigService : ITagsConfigService
{
    private const string ConfigFileName = "tags.json";
    private const string ProductFolder = "Scada.Client";
    
    /// <summary>
    /// Загрузить конфигурацию тегов из файла tags.json
    /// </summary>
    public async Task<TagsConfiguration?> LoadTagsConfigurationAsync()
    {
        // Приоритет 1: Загрузка из AppData (пользовательская копия)
        var appDataPath = GetAppDataTagsFilePath();
        if (File.Exists(appDataPath))
        {
            System.Diagnostics.Debug.WriteLine($"Loading tags from AppData: {appDataPath}");
            return await LoadFromFileAsync(appDataPath);
        }
        
        // Приоритет 2: Загрузка из каталога приложения (встроенный файл)
        var appDirPath = GetAppDirectoryTagsFilePath();
        if (File.Exists(appDirPath))
        {
            System.Diagnostics.Debug.WriteLine($"Loading tags from app directory: {appDirPath}");
            return await LoadFromFileAsync(appDirPath);
        }
        
        System.Diagnostics.Debug.WriteLine("Tags configuration file not found in AppData or app directory");
        return CreateDefaultConfiguration();
    }
    
    /// <summary>
    /// Загрузить конфигурацию из указанного файла
    /// </summary>
    private async Task<TagsConfiguration?> LoadFromFileAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<TagsConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            
            System.Diagnostics.Debug.WriteLine($"Tags configuration loaded: {config?.Tags.Count ?? 0} tags from {filePath}");
            return config ?? CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tags configuration from {filePath}: {ex.Message}");
            return CreateDefaultConfiguration();
        }
    }
    
    /// <summary>
    /// Получить путь к файлу конфигурации тегов в AppData (для пользовательского редактирования)
    /// </summary>
    public string GetAppDataTagsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, ProductFolder);
        return Path.Combine(folder, ConfigFileName);
    }
    
    /// <summary>
    /// Получить путь к файлу конфигурации тегов в каталоге приложения (встроенный)
    /// </summary>
    public string GetAppDirectoryTagsFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, ConfigFileName);
    }
    
    /// <summary>
    /// Получить путь к файлу конфигурации тегов (устаревший метод, оставлен для совместимости)
    /// </summary>
    public string GetTagsConfigFilePath()
    {
        // Возвращаем путь к AppData версии (приоритетный)
        return GetAppDataTagsFilePath();
    }
    
    /// <summary>
    /// Создать конфигурацию по умолчанию (пустую)
    /// </summary>
    private TagsConfiguration CreateDefaultConfiguration()
    {
        return new TagsConfiguration
        {
            Tags = new(),
            Groups = new TagGroups()
        };
    }
}
