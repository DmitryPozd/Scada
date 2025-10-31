using System;
using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Тип элемента на мнемосхеме
/// </summary>
public enum ElementType
{
    CoilButton,
    ImageButton,
    Pump,
    Valve,
    Sensor,
    CoilReadButton,
    InputBitsIndicator
}

/// <summary>
/// Базовый класс для элемента мнемосхемы
/// </summary>
[JsonDerivedType(typeof(CoilElement), typeDiscriminator: "coil")]
[JsonDerivedType(typeof(CoilReadElement), typeDiscriminator: "coilRead")]
[JsonDerivedType(typeof(InputBitsElement), typeDiscriminator: "inputBits")]
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
}

/// <summary>
/// Элемент кнопки чтения катушки
/// </summary>
public class CoilReadElement : MnemoschemeElement
{
    public ushort CoilAddress { get; set; }
    public string? TagName { get; set; }

    public CoilReadElement()
    {
        Type = ElementType.CoilReadButton;
    }
}

/// <summary>
/// Элемент индикатора входных битов
/// </summary>
public class InputBitsElement : MnemoschemeElement
{
    public ushort StartAddress { get; set; }
    public int BitCount { get; set; } = 8;

    public InputBitsElement()
    {
        Type = ElementType.InputBitsIndicator;
    }
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
