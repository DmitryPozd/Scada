using System;
using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Тип элемента на мнемосхеме
/// </summary>
public enum ElementType
{
    CoilButton,
    CoilMomentaryButton, // Устарел: используется только для обратной совместимости, конвертируется в CoilButton с ButtonType.Momentary
    ImageButton,
    Pump,
    Valve,
    Sensor,
    Slider,
    NumericInput,
    Display,
    ImageControl
}

/// <summary>
/// Базовый класс для элемента мнемосхемы
/// </summary>
[JsonDerivedType(typeof(CoilElement), typeDiscriminator: "coil")]
[JsonDerivedType(typeof(PumpElement), typeDiscriminator: "pump")]
[JsonDerivedType(typeof(ValveElement), typeDiscriminator: "valve")]
[JsonDerivedType(typeof(SensorElement), typeDiscriminator: "sensor")]
[JsonDerivedType(typeof(SliderElement), typeDiscriminator: "slider")]
[JsonDerivedType(typeof(NumericInputElement), typeDiscriminator: "numericInput")]
[JsonDerivedType(typeof(DisplayElement), typeDiscriminator: "display")]
[JsonDerivedType(typeof(ImageElement), typeDiscriminator: "image")]
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
    public CoilButtonType ButtonType { get; set; } = CoilButtonType.Toggle; // Тип кнопки: Toggle или Momentary
    public string? ImageType { get; set; } // Для ImageButton: Motor, Valve, Fan, Heater, Light
    public string? IconPathOn { get; set; } // Путь к иконке для состояния ON
    public string? IconPathOff { get; set; } // Путь к иконке для состояния OFF
    public double? ButtonWidth { get; set; } // Ширина для ImageButton
    public double? ButtonHeight { get; set; } // Высота для ImageButton
    public bool ShowLabel { get; set; } = true; // Показывать надпись на кнопке
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

/// <summary>
/// Элемент ползунка для Holding Register
/// </summary>
public class SliderElement : MnemoschemeElement
{
    public ushort RegisterAddress { get; set; }
    public string? TagName { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public string Unit { get; set; } = string.Empty;

    public SliderElement()
    {
        Type = ElementType.Slider;
    }
}

/// <summary>
/// Элемент числового ввода для Holding Register
/// </summary>
public class NumericInputElement : MnemoschemeElement
{
    public ushort RegisterAddress { get; set; }
    public string? TagName { get; set; }
    public string Unit { get; set; } = string.Empty;

    public NumericInputElement()
    {
        Type = ElementType.NumericInput;
    }
}

/// <summary>
/// Элемент отображения для Input Register (только чтение)
/// </summary>
public class DisplayElement : MnemoschemeElement
{
    public ushort RegisterAddress { get; set; }
    public string? TagName { get; set; }
    public string Unit { get; set; } = string.Empty;

    public DisplayElement()
    {
        Type = ElementType.Display;
    }
}

/// <summary>
/// Элемент картинки (изображение без управления)
/// </summary>
public class ImageElement : MnemoschemeElement
{
    public string ImagePath { get; set; } = string.Empty;
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 200;
    public bool ShowLabel { get; set; } = true;

    public ImageElement()
    {
        Type = ElementType.ImageControl;
    }
}
