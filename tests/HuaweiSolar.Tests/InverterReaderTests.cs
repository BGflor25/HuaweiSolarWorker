using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using HuaweiSolar.Worker;
using HuaweiSolar.Worker.Api;
using HuaweiSolar.Worker.Huawei;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HuaweiSolar.Tests;

public class InverterReaderTests
{
    private static InverterReader Reader(FakeModbusServer server) => new(
        Options.Create(new InverterOptions
        {
            Host = "127.0.0.1",
            Port = server.Port,
            ConnectDelay = TimeSpan.Zero,
            RequestTimeout = TimeSpan.FromSeconds(2),
        }),
        NullLogger<InverterReader>.Instance);

    private static JsonObject Python() => JsonNode.Parse(Fixture.Read("python-output.json"))!.AsObject();

    [Fact]
    public async Task Payload_matches_python_output_for_the_same_registers()
    {
        await using var server = FakeModbusServer.WithSampleInverter();

        var payload = PayloadBuilder.Build(await Reader(server).ReadAsync(default));
        var python = Python();

        Assert.Equal(python.Select(p => p.Key), payload.Select(p => p.Key));
        foreach (var (key, expected) in python.Where(p => p.Key != "timestamp"))
        {
            Assert.True(JsonNode.DeepEquals(Normalize(expected), Normalize(payload[key])),
                $"{key}: expected {expected?.ToJsonString()}, got {payload[key]?.ToJsonString()}");
        }
    }

    [Fact]
    public async Task Missing_registers_are_sent_as_null_value_and_unit()
    {
        await using var server = FakeModbusServer.WithSampleInverter();
        foreach (ushort a in new ushort[] { 37760, 37765, 37766 })
            server.Registers.Remove(a);

        var snapshot = await Reader(server).ReadAsync(default);
        var payload = PayloadBuilder.Build(snapshot);

        Assert.Equal(RegisterMap.All.Count - 2, snapshot.Readings.Count);
        Assert.Equal("""{"value":null,"unit":null}""", payload["storage_state_of_capacity"]!.ToJsonString());
        Assert.Equal("""{"value":null,"unit":null}""", payload["storage_charge_discharge_power"]!.ToJsonString());
        Assert.Equal(26.0, payload["internal_temperature"]!["value"]!.GetValue<double>());
    }

    [Fact]
    public async Task Uses_configured_unit_id()
    {
        await using var server = FakeModbusServer.WithSampleInverter();
        var reader = new InverterReader(
            Options.Create(new InverterOptions { Host = "127.0.0.1", Port = server.Port, UnitId = 1, ConnectDelay = TimeSpan.Zero }),
            NullLogger<InverterReader>.Instance);

        await reader.ReadAsync(default);

        Assert.All(server.UnitIdsSeen, id => Assert.Equal(1, id));
    }

    [Fact]
    public async Task Unreachable_inverter_throws()
    {
        int port;
        using (var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            l.Start();
            port = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
        }

        var reader = new InverterReader(
            Options.Create(new InverterOptions { Host = "127.0.0.1", Port = port, ConnectDelay = TimeSpan.Zero }),
            NullLogger<InverterReader>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() => reader.ReadAsync(default));
    }

    // Python writes 26.0 / 0 while System.Text.Json writes 26 / 0; compare numerically.
    private static JsonNode? Normalize(JsonNode? node) => node switch
    {
        JsonValue v when v.GetValueKind() == JsonValueKind.Number =>
            JsonValue.Create(double.Parse(v.ToJsonString(), CultureInfo.InvariantCulture)),
        JsonObject o => new JsonObject(o.Select(p => KeyValuePair.Create(p.Key, Normalize(p.Value)))),
        _ => node?.DeepClone(),
    };
}
