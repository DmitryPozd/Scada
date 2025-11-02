using System;
using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Тип элемента на мнемосхеме
/// </summary>
public enum ElementType
{
    CoilButton,
    CoilMomentaryButton,
    ImageButton,
    Pump,
    Valve,
    Sensor
}

/// <summary>
/// Базовый класс для элемента мнемосхемы
/// </summary>
[JsonDerivedType(typeof(CoilElement), typeDiscriminator: "coil")]
[JsonDerivedType(typeof(PumpElement), typeDiscriminator: "pump")]
[JsonDerivedType(typeof(ValveElement), typeDiscriminator: "valve")]
[JsonDerivedType(typeof(SensorElement), typeDiscriminator: "sensor")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public class MnemoschemeElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ElementType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Элемент управления Coil (CoilButton или ImageButton)
/// </summary>
public class CoilElement : MnemoschemeElement
{
    public ushort CoilAddress { get; set; }
    public string? TagName { get; set; }
    public string? ImageType { get; set; } // Для ImageButton: Motor, Valve, Fan, Heater, Light
    public string? IconPathOn { get; set; } // Путь к иконке для состояния ON
    public string? IconPathOff { get; set; } // Путь к иконке для состояния OFF
}

/// <summary>
/// Элемент насоса
/// </summary>
public class PumpElement : MnemoschemeElement
{
    public PumpElement()
    {
        Type = ElementType.Pump;
    }
}

/// <summary>
/// Элемент клапана
/// </summary>
public class ValveElement : MnemoschemeElement
{
    public ValveElement()
    {
        Type = ElementType.Valve;
    }
}

/// <summary>
/// Элемент датчика
/// </summary>
public class SensorElement : MnemoschemeElement
{
    public string Unit { get; set; } = string.Empty;
    public double? ThresholdLow { get; set; }
    public double? ThresholdHigh { get; set; }

    public SensorElement()
    {
        Type = ElementType.Sensor;
    }
}
