namespace Scada.Client.Models;

/// <summary>
/// Настройки отображения значения регистра на элементе управления
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// Показывать ли значение регистра
    /// </summary>
    public bool ShowValue { get; set; } = false;

    /// <summary>
    /// Адрес регистра для чтения
    /// </summary>
    public ushort RegisterAddress { get; set; } = 0;

    /// <summary>
    /// Тип регистра (Holding, Input, Coil)
    /// </summary>
    public RegisterType RegisterType { get; set; } = RegisterType.Holding;

    /// <summary>
    /// Тип данных регистра
    /// </summary>
    public DataType DataType { get; set; } = DataType.UInt16;

    /// <summary>
    /// Минимальное значение диапазона (для масштабирования)
    /// </summary>
    public double? MinValue { get; set; } = null;

    /// <summary>
    /// Максимальное значение диапазона (для масштабирования)
    /// </summary>
    public double? MaxValue { get; set; } = null;

    /// <summary>
    /// Единицы измерения
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Показывать ли единицы измерения
    /// </summary>
    public bool ShowUnit { get; set; } = true;

    /// <summary>
    /// Количество знаков после запятой
    /// </summary>
    public int DecimalPlaces { get; set; } = 1;

    /// <summary>
    /// Изменять ли цвет текста в зависимости от состояния
    /// </summary>
    public bool ColorByState { get; set; } = false;

    /// <summary>
    /// Цвет текста для состояния OFF (Coil = false)
    /// </summary>
    public string OffStateColor { get; set; } = "#808080"; // Gray

    /// <summary>
    /// Цвет текста для состояния ON (Coil = true)
    /// </summary>
    public string OnStateColor { get; set; } = "#00FF00"; // Green

    /// <summary>
    /// Цвет текста для низких значений (< LowThreshold)
    /// </summary>
    public string LowValueColor { get; set; } = "#0000FF"; // Blue

    /// <summary>
    /// Цвет текста для нормальных значений
    /// </summary>
    public string NormalValueColor { get; set; } = "#00FF00"; // Green

    /// <summary>
    /// Цвет текста для высоких значений (> HighThreshold)
    /// </summary>
    public string HighValueColor { get; set; } = "#FF0000"; // Red

    /// <summary>
    /// Порог низкого значения
    /// </summary>
    public double? LowThreshold { get; set; } = null;

    /// <summary>
    /// Порог высокого значения
    /// </summary>
    public double? HighThreshold { get; set; } = null;

    /// <summary>
    /// Текст для отображения вместо значения при состоянии OFF
    /// </summary>
    public string OffStateText { get; set; } = "OFF";

    /// <summary>
    /// Текст для отображения вместо значения при состоянии ON
    /// </summary>
    public string OnStateText { get; set; } = "ON";

    /// <summary>
    /// Использовать ли текстовые метки вместо значений для Coil
    /// </summary>
    public bool UseStateText { get; set; } = false;

    /// <summary>
    /// Масштабный коэффициент
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Смещение значения
    /// </summary>
    public double Offset { get; set; } = 0.0;
}
