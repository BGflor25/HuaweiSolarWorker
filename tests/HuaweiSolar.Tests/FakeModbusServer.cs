using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HuaweiSolar.Tests;

/// <summary>In-process Modbus TCP server answering function 0x03 from a register bank.</summary>
internal sealed class FakeModbusServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    public Dictionary<ushort, ushort> Registers { get; } = new();

    /// <summary>Requests starting at these addresses get no answer (to test timeouts).</summary>
    public HashSet<ushort> Silent { get; } = new();

    public List<byte> UnitIdsSeen { get; } = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public FakeModbusServer()
    {
        _listener.Start();
        _loop = AcceptLoopAsync();
    }

    public void SetU16(ushort address, ushort value) => Registers[address] = value;

    public void SetI16(ushort address, short value) => Registers[address] = unchecked((ushort)value);

    public void SetU32(ushort address, uint value)
    {
        Registers[address] = (ushort)(value >> 16);
        Registers[(ushort)(address + 1)] = (ushort)value;
    }

    public void SetI32(ushort address, int value) => SetU32(address, unchecked((uint)value));

    public void SetString(ushort address, ushort length, string value)
    {
        var bytes = new byte[length * 2];
        Encoding.ASCII.GetBytes(value).CopyTo(bytes, 0);
        for (var i = 0; i < length; i++)
            Registers[(ushort)(address + i)] = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i * 2));
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleAsync(client);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using var _ = client;
        var stream = client.GetStream();
        var request = new byte[12];
        try
        {
            while (true)
            {
                await stream.ReadExactlyAsync(request, _cts.Token);
                var address = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(8));
                var count = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(10));
                lock (UnitIdsSeen) UnitIdsSeen.Add(request[6]);

                if (Silent.Contains(address))
                    continue;

                byte[] pdu;
                if (Enumerable.Range(address, count).All(a => Registers.ContainsKey((ushort)a)))
                {
                    pdu = new byte[2 + count * 2];
                    pdu[0] = 0x03;
                    pdu[1] = (byte)(count * 2);
                    for (var i = 0; i < count; i++)
                        BinaryPrimitives.WriteUInt16BigEndian(pdu.AsSpan(2 + i * 2), Registers[(ushort)(address + i)]);
                }
                else
                {
                    pdu = [0x83, 0x02]; // illegal data address
                }

                var response = new byte[7 + pdu.Length];
                request.AsSpan(0, 4).CopyTo(response); // transaction + protocol id
                BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(4), (ushort)(pdu.Length + 1));
                response[6] = request[6];
                pdu.CopyTo(response, 7);
                await stream.WriteAsync(response, _cts.Token);
            }
        }
        catch (Exception) { /* client closed or server stopped */ }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        await _loop;
        _cts.Dispose();
    }

    /// <summary>The raw registers behind the provided pv_output.json sample.</summary>
    public static FakeModbusServer WithSampleInverter()
    {
        var s = new FakeModbusServer();
        s.SetString(30000, 15, "SUN2000-4.6KTL-L1");
        s.SetString(30015, 10, "HV22B0338304");
        s.SetU32(30073, 4600);
        s.SetI32(32064, 0);
        s.SetI32(32080, 0);
        s.SetI32(32082, 1);
        s.SetI16(32084, 0);
        s.SetU16(32086, 10000);
        s.SetI16(32087, 260);
        s.SetU16(32089, 0x0200);
        s.SetI32(32078, 2642);
        s.SetU32(32106, 1525022);
        s.SetU32(32114, 118);
        for (ushort a = 32016; a <= 32023; a++)
            s.SetI16(a, 0);
        s.SetU16(32066, 2360);
        s.SetI32(32072, 297);
        s.SetU16(32085, 5002);
        s.SetI32(37107, -266);
        s.SetI32(37132, int.MaxValue); // "not available"
        s.SetI32(37101, 2358);
        s.SetU16(37760, 50);
        s.SetI32(37765, 0);
        s.SetU16(37762, 2);
        s.SetU32(37780, 440696);
        s.SetU32(37782, 433595);
        return s;
    }
}
