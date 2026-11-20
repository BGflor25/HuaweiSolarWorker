using System.Buffers.Binary;
using System.Text;

namespace HuaweiSolar.Worker.Huawei;

public static class RegisterDecoder
{
    public static RegisterReading Decode(RegisterDefinition def, ReadOnlySpan<byte> data)
    {
        if (data.Length != def.Length * 2)
            throw new ArgumentException($"{def.Key}: expected {def.Length * 2} bytes, got {data.Length}", nameof(data));

        object? value = def.Type switch
        {
            RegisterType.String => DecodeString(data),
            RegisterType.DeviceStatus => DeviceStatus.Describe(BinaryPrimitives.ReadUInt16BigEndian(data)),
            RegisterType.U16 => Scale(BinaryPrimitives.ReadUInt16BigEndian(data), ushort.MaxValue, def.Gain),
            RegisterType.I16 => Scale(BinaryPrimitives.ReadInt16BigEndian(data), short.MaxValue, def.Gain),
            RegisterType.U32 => Scale(BinaryPrimitives.ReadUInt32BigEndian(data), uint.MaxValue, def.Gain),
            RegisterType.I32 => Scale(BinaryPrimitives.ReadInt32BigEndian(data), int.MaxValue, def.Gain),
            _ => throw new ArgumentOutOfRangeException(nameof(def), def.Type, null),
        };

        return new RegisterReading(value, def.Unit);
    }

    // Huawei marks "not available" with the type's maximum value.
    private static double? Scale(double raw, double invalid, double gain) =>
        raw == invalid ? null : Math.Round(raw / gain, 6);

    private static string? DecodeString(ReadOnlySpan<byte> data)
    {
        var text = Encoding.ASCII.GetString(data).TrimEnd('\0', ' ');
        var nul = text.IndexOf('\0');
        if (nul >= 0)
            text = text[..nul];
        return text.Length == 0 ? null : text;
    }
}
