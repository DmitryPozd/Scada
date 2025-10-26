namespace Scada.Client.Models;

public enum RegisterType
{
    Holding,
    Input,
    Coils
}

public enum DataType
{
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float32,
    Bool,
    Int64,
    Double
}

public enum WordOrder
{
    // Word order within multi-word values (each word is 16-bit, big-endian inside).
    // HighLow: [HiWord][LoWord] (standard Modbus big-endian words)
    // LowHigh: [LoWord][HiWord] (word-swapped)
    HighLow,
    LowHigh
}
