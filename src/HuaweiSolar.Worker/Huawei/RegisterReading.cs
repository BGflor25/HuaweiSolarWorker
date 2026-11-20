namespace HuaweiSolar.Worker.Huawei;

/// <summary>A decoded register. <see cref="Value"/> is a double, a string or null (invalid / not present).</summary>
public sealed record RegisterReading(object? Value, string? Unit);

/// <summary>One poll of the inverter. Registers that could not be read are missing from <see cref="Readings"/>.</summary>
public sealed record InverterSnapshot(double Timestamp, IReadOnlyDictionary<string, RegisterReading> Readings);
