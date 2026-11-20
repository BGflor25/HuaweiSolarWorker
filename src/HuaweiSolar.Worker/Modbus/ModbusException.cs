namespace HuaweiSolar.Worker.Modbus;

/// <summary>The device answered with a Modbus exception response (e.g. 0x02 illegal data address).</summary>
public sealed class ModbusException(byte exceptionCode)
    : Exception($"Modbus exception 0x{exceptionCode:X2} ({Describe(exceptionCode)})")
{
    public byte ExceptionCode { get; } = exceptionCode;

    private static string Describe(byte code) => code switch
    {
        0x01 => "illegal function",
        0x02 => "illegal data address",
        0x03 => "illegal data value",
        0x04 => "slave device failure",
        0x06 => "slave device busy",
        0x0B => "gateway target device failed to respond",
        _ => "unknown",
    };
}
