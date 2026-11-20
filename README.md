# HuaweiSolar Worker

Read live data from your Huawei SUN2000 solar inverter (and LUNA2000 battery, if you have one) and send
it to your own API every few minutes. That way you can keep a full history in your own time-series
database, without depending on the FusionSolar cloud.

It runs as a small Docker container, for example on a Raspberry Pi.

## How it works

The inverter has its own Wi-Fi access point (`SUN2000-<serial>`). The worker connects to it, reads the
values over Modbus TCP and POSTs them as JSON to an API on your home network.

That means the device running the worker needs to be on **two networks at once**:

```
 Inverter Wi-Fi                     Raspberry Pi                      Home network
 SUN2000-<serial>   ◄── Wi-Fi ───   wlan0          eth0  ─── LAN ──►  your API → time-series DB
 192.168.200.1                      (reads data)   (sends data)
```

- **Wi-Fi → the inverter**, to read the data. This network only reaches the inverter, not your LAN or the internet.
- **Ethernet (or a second Wi-Fi adapter) → your home network**, to send the data to your API.

## What you need

- A Huawei SUN2000 inverter with its Wi-Fi access point enabled
- A Raspberry Pi (or any Linux box) with Wi-Fi **and** Ethernet, running a 64-bit OS
- Docker
- An HTTP endpoint on your network that accepts the JSON (see [What gets sent](#what-gets-sent))

## Setup

### 1. Enable Modbus TCP on the inverter

Open the FusionSolar app, connect to the inverter locally and go to
**Settings → Communication configuration → Modbus TCP**. Set it to **Enable (unrestricted)**.

### 2. Connect the Pi to your home network

Plug in an Ethernet cable. This connection keeps the default route, so the Pi can reach your API and
download Docker images.

### 3. Connect the Pi's Wi-Fi to the inverter

On Raspberry Pi OS (Bookworm):

```bash
sudo nmcli dev wifi connect "SUN2000-<serial>" password "<inverter Wi-Fi password>" ifname wlan0
sudo nmcli con modify "SUN2000-<serial>" ipv4.never-default yes connection.autoconnect yes
sudo nmcli con up "SUN2000-<serial>"
```

> **Don't skip `ipv4.never-default yes`.** Without it the Pi may route all its traffic through the
> inverter's Wi-Fi, which goes nowhere, and your API becomes unreachable.

Check that both sides work:

```bash
ping -c1 192.168.200.1          # the inverter
ping -c1 <your API host>        # your home network
ip route                        # "default via ... dev eth0"
```

### 4. Start the worker

Install Docker if you don't have it yet:

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # then log out and back in
```

Copy [`docker-compose.yml`](docker-compose.yml) to the Pi, set `Inverter__ApiUrl` to your API, and start it:

```bash
docker compose up -d
docker compose logs -f
```

After a few seconds you should see something like:

```
PV On-grid: active 2150 W, battery 87.5 % (-300 W), today 12.4 kWh, 32/32 registers -> HTTP 204
```

The container starts again automatically after a reboot. To update to the latest version:

```bash
docker compose pull && docker compose up -d
```

## Configuration

Set these as environment variables in `docker-compose.yml`:

| Variable | Default | Description |
|---|---|---|
| `Inverter__ApiUrl` | – | **Required.** Where the readings are POSTed |
| `Inverter__Host` | `192.168.200.1` | Inverter address |
| `Inverter__Port` | `6607` | Modbus port. Use `502` if you connect through an SDongle |
| `Inverter__UnitId` | `0` | Modbus unit id. Use `1` with an SDongle |
| `Inverter__Interval` | `00:05:00` | How often to read |
| `TZ` | `Europe/Brussels` | Time zone for the logs |

## What gets sent

Every interval the worker POSTs one JSON object:

```json
{
  "timestamp": 1763640000.123,
  "model_name": { "value": "SUN2000-4.6KTL-L1", "unit": "" },
  "active_power": { "value": 2150, "unit": "W" },
  "daily_yield_energy": { "value": 12.4, "unit": "kWh" },
  "storage_state_of_capacity": { "value": 87.5, "unit": "%" }
}
```

`timestamp` is Unix time in seconds. Every key is always included. If a value isn't available, for
example because you have no battery or power meter, it is sent as `null`, so your API should accept that.

<details>
<summary>All fields</summary>

| Field | Unit |
|---|---|
| `model_name`, `serial_number`, `device_status` | |
| `rated_power`, `input_power`, `active_power`, `day_active_power_peak` | W |
| `reactive_power` | var |
| `power_factor` | |
| `efficiency` | % |
| `internal_temperature` | °C |
| `accumulated_yield_energy`, `daily_yield_energy` | kWh |
| `pv_01_voltage` … `pv_04_voltage` | V |
| `pv_01_current` … `pv_04_current` | A |
| `grid_voltage`, `grid_current`, `grid_frequency` | V, A, Hz |
| `active_grid_A_current`, `active_grid_A_power`, `grid_A_voltage` (needs a power meter) | A, W, V |
| `storage_state_of_capacity` (needs a battery) | % |
| `storage_charge_discharge_power` | W |
| `storage_running_status` | |
| `storage_total_charge`, `storage_total_discharge` | kWh |

The register addresses are in [`RegisterMap.cs`](src/HuaweiSolar.Worker/Huawei/RegisterMap.cs).
</details>

## Troubleshooting

**Can't connect to the inverter (`Connection refused` / timeout)**
Check that the Pi is still on the inverter's Wi-Fi (`ping 192.168.200.1`) and that Modbus TCP is enabled
in the app. The worker simply tries again at the next interval.

**Inverter works, but the API calls fail**
Run `ip route`. The default route should go through `eth0`. If it goes through `wlan0`, repeat the
`ipv4.never-default` step.

**The Pi can reach both networks, but the container can't**
Uncomment `network_mode: host` in `docker-compose.yml`.

**Data stops while the FusionSolar app is open**
The inverter allows only one Modbus connection at a time. Close the app's local connection.

**Some values are always `null`**
That model doesn't have those registers. Add `Logging__LogLevel__HuaweiSolar.Worker.Huawei: Debug` to see which ones.

## Development

```bash
dotnet test                                      # runs against a built-in fake inverter
dotnet run --project src/HuaweiSolar.Worker      # needs access to the inverter and your API
```

Docker images for `amd64` and `arm64` are published to `ghcr.io/bgflor25/huaweisolarworker` on every
push to `main` (`latest`) and for every `vX.Y.Z` tag.
