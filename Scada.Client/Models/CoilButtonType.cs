namespace Scada.Client.Models;

/// <summary>
/// Тип поведения кнопки Coil
/// </summary>
public enum CoilButtonType
{
    /// <summary>
    /// Кнопка с фиксацией (переключатель ON/OFF)
    /// </summary>
    Toggle = 0,
    
    /// <summary>
    /// Моментальная кнопка (активна только пока нажата)
    /// </summary>
    Momentary = 1
}
