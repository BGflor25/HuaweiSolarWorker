using HuaweiSolar.Worker.Api;
using HuaweiSolar.Worker.Huawei;
using Microsoft.Extensions.Options;

namespace HuaweiSolar.Worker;

public sealed class InverterWorker(
    IInverterReader reader,
    PvApiClient api,
    IOptions<InverterOptions> options,
    ILogger<InverterWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reading inverter {Host}:{Port} (unit {UnitId}) every {Interval}, posting to {Url}",
            options.Value.Host, options.Value.Port, options.Value.UnitId, options.Value.Interval, options.Value.ApiUrl);

        using var timer = new PeriodicTimer(options.Value.Interval);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            var snapshot = await reader.ReadAsync(ct);
            if (snapshot.Readings.Count == 0)
            {
                logger.LogWarning("Connected to the inverter but no register could be read; nothing posted");
                return;
            }

            var status = await api.PostAsync(PayloadBuilder.Build(snapshot), ct);

            logger.LogInformation(
                "PV {Status}: active {Active} W, battery {Soc} % ({Battery} W), today {Daily} kWh, {Read}/{Total} registers -> HTTP {Http}",
                Value(snapshot, "device_status"), Value(snapshot, "active_power"),
                Value(snapshot, "storage_state_of_capacity"), Value(snapshot, "storage_charge_discharge_power"),
                Value(snapshot, "daily_yield_energy"), snapshot.Readings.Count, RegisterMap.All.Count, (int)status);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Inverter cycle failed");
        }
    }

    private static object Value(InverterSnapshot snapshot, string key) =>
        snapshot.Readings.TryGetValue(key, out var r) && r.Value is not null ? r.Value : "n/a";
}
