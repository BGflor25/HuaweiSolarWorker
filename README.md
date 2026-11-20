# HuaweiSolar worker

.NET 10 background service that reads a Huawei SUN2000 inverter (with LUNA2000 battery) over Modbus TCP
and POSTs the readings to the PV-installation API every 5 minutes. It replaces `run_pv_datacollector.sh` +
`get_pv_data.py` (huawei_solar) + cron. The JSON payload has the same keys, order and values as the old
`pv_output.json`.

## Deploy on the Raspberry Pi
```bash
uname -m                        # aarch64 expected (64-bit Pi OS)
ping -c1 192.168.200.1          # the Pi must be connected to the inverter's Wi-Fi access point

crontab -e                      # disable the run_pv_datacollector.sh line first (see Notes)
docker compose pull             # latest image from ghcr.io
docker compose up -d
docker compose logs -f          # expect: "PV On-grid: active ... W, battery ... % ..., 32/32 registers -> HTTP 204"
```
Compare a few values with the last `pv_full_data.json` from the Python script. If they match, you can delete
`get_pv_data.py`, `run_pv_datacollector.sh` and the `pv_*.json(l)` / `pv_full_pipeline.log` files.

Update later with `docker compose pull && docker compose up -d`. To build on the Pi itself instead of
pulling, use `docker compose up -d --build`.

Building on the Mac instead: `docker buildx build --platform linux/arm64 -t huaweisolar-worker src/HuaweiSolar.Worker`.

## Published image
`.github/workflows/docker-publish.yml` runs the tests and builds `ghcr.io/bgflor25/huaweisolarworker`
for `linux/amd64` and `linux/arm64`:
- push to `main` → tags `latest` and `sha-<commit>`
- tag `vX.Y.Z` → tags `X.Y.Z` and `X.Y`
- pull requests → build only, nothing published

The image name comes from the GitHub repository name, so create the repo as `HuaweiSolarWorker`.
If the package is private, log in on the Pi first with a token that has `read:packages`:
`echo <token> | docker login ghcr.io -u BGflor25 --password-stdin`.

## Configuration
`appsettings.json` → `Inverter` section, overridable with env vars:

| Env var | Default | Was in the Python setup |
|---|---|---|
| `Inverter__Host` | `192.168.200.1` | `INVERTER_HOST` |
| `Inverter__Port` | `6607` | `INVERTER_PORT` |
| `Inverter__UnitId` | `0` | huawei_solar default slave id (use `1` behind an SDongle on port 502) |
| `Inverter__ApiUrl` | `http://192.168.20.157:8080/api/pvinstallation/upload` | curl URL in `run_pv_datacollector.sh` |
| `Inverter__Interval` | `00:05:00` | cron schedule |
| `Inverter__ConnectDelay` | `00:00:01` | – (the inverter ignores requests sent right after connecting) |
| `Inverter__RequestTimeout` | `00:00:05` | – (connect and per-register timeout) |
| `TZ` | `Europe/Brussels` (compose) | – |

`SNAPSHOT_FILE`, `HISTORY_FILE`, `API_PAYLOAD_FILE` and `LOG_FILE` are gone: the data goes straight to the
API, and logs go to the console, where Docker rotates them (5 MB × 3).

Set `Logging__LogLevel__HuaweiSolar.Worker.Huawei=Debug` to see which registers this model doesn't support.

## What is read
Every register is read separately, so a register this model doesn't have only loses that value. Like
before, a value that couldn't be read is sent as `{"value": null, "unit": null}`. A register that reports
Huawei's "not available" marker (e.g. `active_grid_A_power` without a meter) is sent as `null` with its unit.

| Key | Register | Type / gain | Unit |
|---|---|---|---|
| `model_name` / `serial_number` | 30000 / 30015 | string | |
| `rated_power` | 30073 | U32 | W |
| `input_power`, `active_power`, `reactive_power` | 32064, 32080, 32082 | I32 | W, W, var |
| `power_factor` | 32084 | I16 / 1000 | |
| `efficiency` | 32086 | U16 / 100 | % |
| `internal_temperature` | 32087 | I16 / 10 | °C |
| `device_status` | 32089 | status code → text (e.g. `On-grid`) | |
| `day_active_power_peak` | 32078 | I32 | W |
| `accumulated_yield_energy`, `daily_yield_energy` | 32106, 32114 | U32 / 100 | kWh |
| `pv_01..04_voltage` / `_current` | 32016–32023 | I16 / 10, I16 / 100 | V, A |
| `grid_voltage`, `grid_current`, `grid_frequency` | 32066, 32072, 32085 | U16 / 10, I32 / 1000, U16 / 100 | V, A, Hz |
| `active_grid_A_current`, `active_grid_A_power`, `grid_A_voltage` | 37107, 37132, 37101 | I32 / 100, I32, I32 / 10 | A, W, V (power meter) |
| `storage_state_of_capacity` | 37760 | U16 / 10 | % |
| `storage_running_status` | 37762 | U16 (raw code) | |
| `storage_charge_discharge_power` | 37765 | I32 | W |
| `storage_total_charge`, `storage_total_discharge` | 37780, 37782 | U32 / 100 | kWh |

The register list lives in `src/HuaweiSolar.Worker/Huawei/RegisterMap.cs`.

## Local run
```bash
Inverter__Host=192.168.200.1 dotnet run --project src/HuaweiSolar.Worker
```

## Notes
- The inverter accepts only one Modbus client at a time. Disable the cron job before starting the
  container. Also close the FusionSolar app's local connection, or the two will compete.
- If the worker logs `Connection refused` or timeouts, check that the Pi is still connected to the inverter's
  Wi-Fi (`SUN2000-...`). The worker retries on the next interval.
- `timestamp` is the Unix time of the reading in seconds, like Python's `time.time()`.

## Tests
`dotnet test`. `tests/HuaweiSolar.Tests` runs an in-process fake Modbus server loaded with the raw registers
behind the sample `pv_output.json` (`Fixtures/python-output.json`). It checks that the C# payload matches
the Python output key for key. Other tests cover the register decoder and the Modbus client
(exception responses, timeouts).
