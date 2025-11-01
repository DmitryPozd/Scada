using System.Collections.Generic;

namespace Scada.Client.Models;

/// <summary>
/// Конфигурация всех тегов системы (загружается из tags.json)
/// </summary>
public class TagsConfiguration
{
    /// <summary>
    /// Список всех тегов (биты и регистры)
    /// </summary>
    public List<TagDefinition> Tags { get; set; } = new();
    
    /// <summary>
    /// Группировка тегов по категориям для удобства
    /// </summary>
    public TagGroups? Groups { get; set; }
}

/// <summary>
/// Группировка тегов по функциональному назначению
/// </summary>
public class TagGroups
{
    /// <summary>
    /// Дискретные входы (X0-X7, X8-X15, и т.д.)
    /// </summary>
    public List<string>? DigitalInputs { get; set; }
    
    /// <summary>
    /// Дискретные выходы (Y0-Y7, Y8-Y15, и т.д.)
    /// </summary>
    public List<string>? DigitalOutputs { get; set; }
    
    /// <summary>
    /// Внутренние флаги (M0-M7, M8-M15, и т.д.)
    /// </summary>
    public List<string>? InternalFlags { get; set; }
    
    /// <summary>
    /// Аналоговые входы
    /// </summary>
    public List<string>? AnalogInputs { get; set; }
    
    /// <summary>
    /// Аналоговые выходы
    /// </summary>
    public List<string>? AnalogOutputs { get; set; }
}
