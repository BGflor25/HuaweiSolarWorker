using System.Buffers.Binary;
using System.Net.Sockets;

namespace HuaweiSolar.Worker.Modbus;

/// <summary>
/// Minimal Modbus TCP client: only function 0x03 (Read Holding Registers), which is all the
/// Huawei SUN2000 needs for reading telemetry.
/// </summary>
public sealed class ModbusTcpClient : IAsyncDisposable
{
    private const byte ReadHoldingRegisters = 0x03;
    private const int MbapHeaderLength = 7;

    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private ushort _transactionId;

    private ModbusTcpClient(TcpClient tcp)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
    }

    public static async Task<ModbusTcpClient> ConnectAsync(string host, int port, TimeSpan timeout, CancellationToken ct)
    {
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            await tcp.ConnectAsync(host, port, cts.Token);
            return new ModbusTcpClient(tcp);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            tcp.Dispose();
            throw new TimeoutException($"Connecting to {host}:{port} timed out after {timeout.TotalSeconds:0.#}s");
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    /// <summary>Reads <paramref name="count"/> registers and returns their raw big-endian bytes.</summary>
    public async Task<byte[]> ReadHoldingRegistersAsync(
        byte unitId, ushort address, ushort count, TimeSpan timeout, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, (ushort)125);

        var transactionId = ++_transactionId;
        var request = new byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(0), transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(2), 0); // protocol id
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(4), 6); // remaining length
        request[6] = unitId;
        request[7] = ReadHoldingRegisters;
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(8), address);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(10), count);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            await _stream.WriteAsync(request, cts.Token);

            while (true)
            {
                var (responseId, pdu) = await ReadFrameAsync(cts.Token);
                if (responseId != transactionId)
                    continue; // late answer to an earlier, timed-out request

                return ParseReadResponse(pdu, count);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Reading register {address} timed out after {timeout.TotalSeconds:0.#}s");
        }
    }

    private async Task<(ushort TransactionId, byte[] Pdu)> ReadFrameAsync(CancellationToken ct)
    {
        var header = new byte[MbapHeaderLength];
        await _stream.ReadExactlyAsync(header, ct);

        var transactionId = BinaryPrimitives.ReadUInt16BigEndian(header);
        var length = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4));
        if (length is < 2 or > 254)
            throw new IOException($"Invalid Modbus frame length {length}");

        var pdu = new byte[length - 1]; // length includes the unit id already read
        await _stream.ReadExactlyAsync(pdu, ct);
        return (transactionId, pdu);
    }

    private static byte[] ParseReadResponse(byte[] pdu, ushort count)
    {
        if (pdu[0] == (ReadHoldingRegisters | 0x80))
            throw new ModbusException(pdu.Length > 1 ? pdu[1] : (byte)0);

        if (pdu[0] != ReadHoldingRegisters)
            throw new IOException($"Unexpected Modbus function code 0x{pdu[0]:X2}");

        var byteCount = pdu.Length > 1 ? pdu[1] : 0;
        if (byteCount != count * 2 || pdu.Length < 2 + byteCount)
            throw new IOException($"Expected {count * 2} data bytes, got {byteCount}");

        return pdu[2..(2 + byteCount)];
    }

    public ValueTask DisposeAsync()
    {
        _tcp.Dispose();
        return ValueTask.CompletedTask;
    }
}
