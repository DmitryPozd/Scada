namespace Scada.Client.Models;

/// <summary>
/// Предпочтение темы приложения.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Системная тема (следует за Windows)
    /// </summary>
    System = 0,
    
    /// <summary>
    /// Светлая тема
    /// </summary>
    Light = 1,
    
    /// <summary>
    /// Тёмная тема
    /// </summary>
    Dark = 2
}
