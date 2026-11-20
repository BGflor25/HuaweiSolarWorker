using HuaweiSolar.Worker.Modbus;
using Microsoft.Extensions.Options;

namespace HuaweiSolar.Worker.Huawei;

public sealed class InverterReader(
    IOptions<InverterOptions> options,
    ILogger<InverterReader> logger,
    TimeProvider? timeProvider = null) : IInverterReader
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    public async Task<InverterSnapshot> ReadAsync(CancellationToken ct)
    {
        var o = options.Value;
        var timestamp = _time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0;

        await using var client = await ModbusTcpClient.ConnectAsync(o.Host, o.Port, o.RequestTimeout, ct);
        if (o.ConnectDelay > TimeSpan.Zero)
            await Task.Delay(o.ConnectDelay, _time, ct);

        // Read one register at a time, like the Python script: a register this model doesn't
        // have only loses that value instead of failing the whole batch.
        var readings = new Dictionary<string, RegisterReading>();
        foreach (var def in RegisterMap.All)
        {
            try
            {
                var data = await client.ReadHoldingRegistersAsync(o.UnitId, def.Address, def.Length, o.RequestTimeout, ct);
                readings[def.Key] = RegisterDecoder.Decode(def, data);
            }
            catch (Exception ex) when (ex is ModbusException or TimeoutException or IOException && !ct.IsCancellationRequested)
            {
                logger.LogDebug("Register {Key} ({Address}) unavailable: {Error}", def.Key, def.Address, ex.Message);
            }
        }

        return new InverterSnapshot(timestamp, readings);
    }
}
