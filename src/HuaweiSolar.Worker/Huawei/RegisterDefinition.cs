namespace HuaweiSolar.Worker.Huawei;

public enum RegisterType
{
    String,
    U16,
    I16,
    U32,
    I32,
    DeviceStatus,
}

/// <param name="Key">Key in the /api/pvinstallation/upload payload.</param>
/// <param name="Address">Modbus holding-register address.</param>
/// <param name="Length">Number of 16-bit registers.</param>
/// <param name="Gain">Raw value is divided by this.</param>
public sealed record RegisterDefinition(
    string Key,
    ushort Address,
    ushort Length,
    RegisterType Type,
    double Gain = 1,
    string? Unit = null)
{
    public static RegisterDefinition Str(string key, ushort address, ushort length) =>
        new(key, address, length, RegisterType.String);

    public static RegisterDefinition U16(string key, ushort address, double gain = 1, string? unit = null) =>
        new(key, address, 1, RegisterType.U16, gain, unit);

    public static RegisterDefinition I16(string key, ushort address, double gain = 1, string? unit = null) =>
        new(key, address, 1, RegisterType.I16, gain, unit);

    public static RegisterDefinition U32(string key, ushort address, double gain = 1, string? unit = null) =>
        new(key, address, 2, RegisterType.U32, gain, unit);

    public static RegisterDefinition I32(string key, ushort address, double gain = 1, string? unit = null) =>
        new(key, address, 2, RegisterType.I32, gain, unit);
}
