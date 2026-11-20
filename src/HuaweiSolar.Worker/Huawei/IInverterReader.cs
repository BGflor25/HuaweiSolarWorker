namespace HuaweiSolar.Worker.Huawei;

public interface IInverterReader
{
    /// <summary>Connects, reads every register in <see cref="RegisterMap"/> and disconnects.</summary>
    /// <exception cref="Exception">The inverter could not be reached at all.</exception>
    Task<InverterSnapshot> ReadAsync(CancellationToken ct);
}
