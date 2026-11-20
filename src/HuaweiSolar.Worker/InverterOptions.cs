namespace HuaweiSolar.Worker;

public sealed class InverterOptions
{
    public const string SectionName = "Inverter";

    /// <summary>Inverter address. 192.168.200.1 is the inverter's own Wi-Fi access point.</summary>
    public string Host { get; set; } = "192.168.200.1";

    /// <summary>Modbus TCP port. 6607 on the inverter access point, 502 on an SDongle.</summary>
    public int Port { get; set; } = 6607;

    /// <summary>Modbus unit (slave) id. 0 on the inverter access point, usually 1 behind an SDongle.</summary>
    public byte UnitId { get; set; }

    /// <summary>The inverter ignores requests sent right after the connection opens.</summary>
    public TimeSpan ConnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Timeout for connecting and for each register read.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Time between readings.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Endpoint the JSON payload is POSTed to.</summary>
    public Uri? ApiUrl { get; set; }
}
