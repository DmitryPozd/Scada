using System.Threading.Tasks;
using Scada.Client.Models;

namespace Scada.Client.Services;

/// <summary>
/// Интерфейс сервиса для загрузки конфигурации тегов
/// </summary>
public interface ITagsConfigService
{
    /// <summary>
    /// Загрузить конфигурацию тегов из файла tags.json
    /// </summary>
    /// <returns>Конфигурация тегов или null при ошибке</returns>
    Task<TagsConfiguration?> LoadTagsConfigurationAsync();
    
    /// <summary>
    /// Получить путь к файлу конфигурации тегов
    /// </summary>
    string GetTagsConfigFilePath();
}
