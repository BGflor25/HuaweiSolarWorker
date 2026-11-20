namespace HuaweiSolar.Worker.Huawei;

/// <summary>Register 32089 (device status) codes and their descriptions.</summary>
public static class DeviceStatus
{
    private static readonly Dictionary<ushort, string> Texts = new()
    {
        [0x0000] = "Standby: initializing",
        [0x0001] = "Standby: detecting insulation resistance",
        [0x0002] = "Standby: detecting irradiation",
        [0x0003] = "Standby: grid detecting",
        [0x0100] = "Starting",
        [0x0200] = "On-grid",
        [0x0201] = "Grid Connection: power limited",
        [0x0202] = "Grid Connection: self-derating",
        [0x0203] = "Off-grid Running",
        [0x0300] = "Shutdown: fault",
        [0x0301] = "Shutdown: command",
        [0x0302] = "Shutdown: OVGR",
        [0x0303] = "Shutdown: communication disconnected",
        [0x0304] = "Shutdown: power limited",
        [0x0305] = "Shutdown: manual startup required",
        [0x0306] = "Shutdown: DC switches disconnected",
        [0x0307] = "Shutdown: rapid cutoff",
        [0x0308] = "Shutdown: input underpowered",
        [0x0401] = "Grid scheduling: cosphi-P curve",
        [0x0402] = "Grid scheduling: Q-U curve",
        [0x0403] = "Grid scheduling: PF-U curve",
        [0x0404] = "Grid scheduling: dry contact",
        [0x0405] = "Grid scheduling: Q-P curve",
        [0x0500] = "Spot-check ready",
        [0x0501] = "Spot-checking",
        [0x0600] = "Inspecting",
        [0x0700] = "AFCI self check",
        [0x0800] = "I-V scanning",
        [0x0900] = "DC input detection",
        [0x0A00] = "Running: off-grid charging",
        [0xA000] = "Standby: no irradiation",
    };

    public static string Describe(ushort code) =>
        Texts.TryGetValue(code, out var text) ? text : $"Unknown (0x{code:X4})";
}
