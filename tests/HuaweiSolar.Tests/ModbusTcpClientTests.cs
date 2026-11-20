using HuaweiSolar.Worker.Modbus;

namespace HuaweiSolar.Tests;

public class ModbusTcpClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Reads_multiple_registers_big_endian()
    {
        await using var server = new FakeModbusServer();
        server.SetU32(100, 0x01020304);

        await using var client = await ModbusTcpClient.ConnectAsync("127.0.0.1", server.Port, Timeout, default);
        var data = await client.ReadHoldingRegistersAsync(7, 100, 2, Timeout, default);

        Assert.Equal([0x01, 0x02, 0x03, 0x04], data);
        Assert.Equal([(byte)7], server.UnitIdsSeen);
    }

    [Fact]
    public async Task Exception_response_throws_ModbusException()
    {
        await using var server = new FakeModbusServer();
        await using var client = await ModbusTcpClient.ConnectAsync("127.0.0.1", server.Port, Timeout, default);

        var ex = await Assert.ThrowsAsync<ModbusException>(() => client.ReadHoldingRegistersAsync(0, 500, 1, Timeout, default));
        Assert.Equal(0x02, ex.ExceptionCode);
    }

    [Fact]
    public async Task No_answer_times_out_and_next_read_still_works()
    {
        await using var server = new FakeModbusServer();
        server.Silent.Add(1);
        server.SetU16(2, 42);
        await using var client = await ModbusTcpClient.ConnectAsync("127.0.0.1", server.Port, Timeout, default);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.ReadHoldingRegistersAsync(0, 1, 1, TimeSpan.FromMilliseconds(200), default));

        Assert.Equal([0x00, 0x2A], await client.ReadHoldingRegistersAsync(0, 2, 1, Timeout, default));
    }
}
