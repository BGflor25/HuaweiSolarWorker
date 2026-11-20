using HuaweiSolar.Worker.Huawei;

namespace HuaweiSolar.Tests;

public class RegisterDecoderTests
{
    private static RegisterReading Decode(RegisterDefinition def, params byte[] data) => RegisterDecoder.Decode(def, data);

    [Fact]
    public void U32_applies_gain_and_keeps_unit()
    {
        var r = Decode(RegisterDefinition.U32("e", 0, gain: 100, unit: "kWh"), 0x00, 0x17, 0x45, 0x1E);
        Assert.Equal(15250.22, r.Value);
        Assert.Equal("kWh", r.Unit);
    }

    [Fact]
    public void I32_negative_value() =>
        Assert.Equal(-2.66, Decode(RegisterDefinition.I32("i", 0, gain: 100), 0xFF, 0xFF, 0xFE, 0xF6).Value);

    [Fact]
    public void I16_negative_value() =>
        Assert.Equal(-1.5, Decode(RegisterDefinition.I16("t", 0, gain: 10), 0xFF, 0xF1).Value);

    [Theory]
    [InlineData(RegisterType.U16, new byte[] { 0xFF, 0xFF })]
    [InlineData(RegisterType.I16, new byte[] { 0x7F, 0xFF })]
    [InlineData(RegisterType.U32, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    [InlineData(RegisterType.I32, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
    public void Invalid_marker_decodes_to_null_but_keeps_unit(RegisterType type, byte[] data)
    {
        var r = RegisterDecoder.Decode(new RegisterDefinition("x", 0, (ushort)(data.Length / 2), type, 1, "W"), data);
        Assert.Null(r.Value);
        Assert.Equal("W", r.Unit);
    }

    [Fact]
    public void String_is_trimmed_at_first_nul()
    {
        var data = new byte[8];
        "AB C"u8.CopyTo(data);
        data[6] = (byte)'Z'; // garbage after the terminator
        Assert.Equal("AB C", Decode(RegisterDefinition.Str("s", 0, 4), data).Value);
    }

    [Fact]
    public void Empty_string_is_null() =>
        Assert.Null(Decode(RegisterDefinition.Str("s", 0, 2), 0, 0, 0, 0).Value);

    [Theory]
    [InlineData(0x0200, "On-grid")]
    [InlineData(0xA000, "Standby: no irradiation")]
    [InlineData(0x1234, "Unknown (0x1234)")]
    public void Device_status_text(int code, string expected) =>
        Assert.Equal(expected, Decode(new RegisterDefinition("d", 0, 1, RegisterType.DeviceStatus), (byte)(code >> 8), (byte)code).Value);

    [Fact]
    public void Wrong_length_throws() =>
        Assert.Throws<ArgumentException>(() => Decode(RegisterDefinition.U32("x", 0), 0, 0));

    [Fact]
    public void Register_map_keys_are_unique() =>
        Assert.Equal(RegisterMap.All.Count, RegisterMap.All.Select(r => r.Key).Distinct().Count());
}
