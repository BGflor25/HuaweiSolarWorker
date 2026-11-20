using static HuaweiSolar.Worker.Huawei.RegisterDefinition;

namespace HuaweiSolar.Worker.Huawei;

/// <summary>
/// The registers that are read, in payload order. Addresses, types and gains follow Huawei's
/// "SUN2000 Modbus Interface Definitions". Not every register exists on every model;
/// meter registers (371xx) need a smart power sensor, storage registers (377xx) a LUNA battery.
/// </summary>
public static class RegisterMap
{
    public static IReadOnlyList<RegisterDefinition> All { get; } =
    [
        Str("model_name", 30000, 15),
        Str("serial_number", 30015, 10),
        U32("rated_power", 30073, unit: "W"),
        I32("input_power", 32064, unit: "W"),
        I32("active_power", 32080, unit: "W"),
        I32("reactive_power", 32082, unit: "var"),
        I16("power_factor", 32084, gain: 1000),
        U16("efficiency", 32086, gain: 100, unit: "%"),
        I16("internal_temperature", 32087, gain: 10, unit: "°C"),
        new("device_status", 32089, 1, RegisterType.DeviceStatus),
        I32("day_active_power_peak", 32078, unit: "W"),
        U32("accumulated_yield_energy", 32106, gain: 100, unit: "kWh"),
        U32("daily_yield_energy", 32114, gain: 100, unit: "kWh"),

        I16("pv_01_voltage", 32016, gain: 10, unit: "V"),
        I16("pv_01_current", 32017, gain: 100, unit: "A"),
        I16("pv_02_voltage", 32018, gain: 10, unit: "V"),
        I16("pv_02_current", 32019, gain: 100, unit: "A"),
        I16("pv_03_voltage", 32020, gain: 10, unit: "V"),
        I16("pv_03_current", 32021, gain: 100, unit: "A"),
        I16("pv_04_voltage", 32022, gain: 10, unit: "V"),
        I16("pv_04_current", 32023, gain: 100, unit: "A"),

        U16("grid_voltage", 32066, gain: 10, unit: "V"),
        I32("grid_current", 32072, gain: 1000, unit: "A"),
        U16("grid_frequency", 32085, gain: 100, unit: "Hz"),

        // Power meter (smart power sensor) phase A
        I32("active_grid_A_current", 37107, gain: 100, unit: "A"),
        I32("active_grid_A_power", 37132, unit: "W"),
        I32("grid_A_voltage", 37101, gain: 10, unit: "V"),

        // Battery (LUNA2000)
        U16("storage_state_of_capacity", 37760, gain: 10, unit: "%"),
        I32("storage_charge_discharge_power", 37765, unit: "W"),
        U16("storage_running_status", 37762),
        U32("storage_total_charge", 37780, gain: 100, unit: "kWh"),
        U32("storage_total_discharge", 37782, gain: 100, unit: "kWh"),
    ];
}
