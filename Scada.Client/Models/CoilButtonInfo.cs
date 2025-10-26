namespace Scada.Client.Models;

/// <summary>
/// Информация о кнопке управления Coil для копирования/вставки
/// </summary>
public class CoilButtonInfo
{
    public string Label { get; set; } = string.Empty;
    public ushort CoilAddress { get; set; }
    public string? TagName { get; set; }
    public bool IsImageButton { get; set; }
    public string? ImageType { get; set; } // "Motor", "Valve", "Fan", etc.
    public double X { get; set; }
    public double Y { get; set; }
}
