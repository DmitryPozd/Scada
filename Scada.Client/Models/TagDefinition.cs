using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Helper class to determine allowed data types for tags based on their prefix
/// </summary>
public static class TagDataTypeRules
{
    /// <summary>
    /// Get allowed data types for a tag based on its name prefix
    /// </summary>
    public static DataType[] GetAllowedDataTypes(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return new[] { DataType.Bool };

        // X, Y, M, T, C, SM, S - только Bool
        if (tagName.StartsWith("X") || tagName.StartsWith("Y") || 
            tagName.StartsWith("M") || tagName.StartsWith("T") || 
            tagName.StartsWith("C") || tagName.StartsWith("SM") || 
            tagName.StartsWith("S"))
        {
            return new[] { DataType.Bool };
        }

        // AI, AQ - Int16 (16-bit signed integer, 1 register, -32768~32767)
        if (tagName.StartsWith("AI") || tagName.StartsWith("AQ"))
        {
            return new[] { DataType.Int16 };
        }

        // TV, CV, SV - Int16 или Int32
        if (tagName.StartsWith("TV") || tagName.StartsWith("CV") || tagName.StartsWith("SV"))
        {
            return new[] { DataType.Int16, DataType.Int32 };
        }

        // V - Int16, Int32, Float32, или CHAR (пока используем Int16 для CHAR)
        if (tagName.StartsWith("V"))
        {
            return new[] { DataType.Int16, DataType.Int32, DataType.Float32 };
        }

        // По умолчанию - UInt16
        return new[] { DataType.UInt16 };
    }

    /// <summary>
    /// Get default data type for a tag based on its name prefix
    /// </summary>
    public static DataType GetDefaultDataType(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return DataType.Bool;

        if (tagName.StartsWith("X") || tagName.StartsWith("Y") || 
            tagName.StartsWith("M") || tagName.StartsWith("T") || 
            tagName.StartsWith("C") || tagName.StartsWith("SM") || 
            tagName.StartsWith("S"))
        {
            return DataType.Bool;
        }

        if (tagName.StartsWith("AI") || tagName.StartsWith("AQ") ||
            tagName.StartsWith("TV") || tagName.StartsWith("CV") || 
            tagName.StartsWith("SV") || tagName.StartsWith("V"))
        {
            return DataType.Int16;
        }

        return DataType.UInt16;
    }
}

/// <summary>
/// Defines a Modbus holding register tag with optional linear scaling.
/// </summary>
public class TagDefinition
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public ushort Address { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RegisterType Register { get; set; } = RegisterType.Holding;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataType Type { get; set; } = DataType.UInt16;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WordOrder WordOrder { get; set; } = WordOrder.HighLow;
    
    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;

    [JsonIgnore]
    public double Value { get; set; }

    /// <summary>
    /// Список допустимых типов данных для этого тега (используется в UI)
    /// </summary>
    [JsonIgnore]
    public DataType[] AllowedDataTypes => TagDataTypeRules.GetAllowedDataTypes(Name);
}
