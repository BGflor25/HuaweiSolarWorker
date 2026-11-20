using System.Text.Json.Nodes;
using HuaweiSolar.Worker.Huawei;

namespace HuaweiSolar.Worker.Api;

public static class PayloadBuilder
{
    /// <summary>
    /// Builds the /api/pvinstallation/upload body: <c>timestamp</c> followed by every key of
    /// <see cref="RegisterMap"/> as <c>{"value", "unit"}</c>. Registers that could not be read
    /// are sent as <c>{"value": null, "unit": null}</c>, exactly like build_api_payload in get_pv_data.py.
    /// </summary>
    public static JsonObject Build(InverterSnapshot snapshot)
    {
        var payload = new JsonObject { ["timestamp"] = snapshot.Timestamp };

        foreach (var def in RegisterMap.All)
        {
            snapshot.Readings.TryGetValue(def.Key, out var reading);
            payload[def.Key] = new JsonObject
            {
                ["value"] = reading?.Value switch
                {
                    double d => JsonValue.Create(d),
                    string s => JsonValue.Create(s),
                    _ => null,
                },
                ["unit"] = reading?.Unit,
            };
        }

        return payload;
    }
}
