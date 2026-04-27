using FreeRedis;

namespace FreeIM.Tests;

/// <summary>
/// 类型转换逻辑单元测试
/// </summary>
public class TypeConversionTests
{
    private readonly RedisClient _redis = new RedisClient("127.0.0.1:6379");

    [Fact]
    public void ConvertToInt64_Long_ShouldReturnSameValue()
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<long>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [12345L]);
        Assert.Equal(12345L, result);
    }

    [Fact]
    public void ConvertToInt64_Int_ShouldReturnCorrectValue()
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<int>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [12345]);
        Assert.Equal(12345L, result);
    }

    [Fact]
    public void ConvertToInt64_Short_ShouldReturnCorrectValue()
    {
        var client = new ImClient<short>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<short>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [(short)12345]);
        Assert.Equal(12345L, result);
    }

    [Fact]
    public void ConvertToInt64_Byte_ShouldReturnCorrectValue()
    {
        var client = new ImClient<byte>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<byte>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [(byte)255]);
        Assert.Equal(255L, result);
    }

    [Fact]
    public void ConvertToClientIdType_Long_ShouldReturnSameValue()
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<long>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [12345L]);
        Assert.Equal(12345L, result);
    }

    [Fact]
    public void ConvertToClientIdType_Int_ShouldReturnCorrectValue()
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<int>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [12345L]);
        Assert.Equal(12345, result);
    }

    [Fact]
    public void ConvertToClientIdType_Short_ShouldReturnCorrectValue()
    {
        var client = new ImClient<short>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<short>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [12345L]);
        Assert.Equal((short)12345, result);
    }

    [Fact]
    public void ConvertToClientIdType_Byte_ShouldReturnCorrectValue()
    {
        var client = new ImClient<byte>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var method = typeof(ImClient<byte>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(client, [255L]);
        Assert.Equal((byte)255, result);
    }

    [Fact]
    public void GetChanClientList_Long_ShouldReturnLongArray()
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var result = client.GetChanClientList("channel1");
        Assert.IsType<long[]>(result);
    }

    [Fact]
    public void GetChanClientList_Int_ShouldReturnIntArray()
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var result = client.GetChanClientList("channel1");
        Assert.IsType<int[]>(result);
    }

    [Fact]
    public void SendMessage_Long_ReceiverShouldBeLong()
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        client.SendMessage(100L, new long[] { 1L, 2L, 3L }, new { message = "test" });
    }

    [Fact]
    public void SendMessage_Int_ReceiverShouldBeInt()
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        client.SendMessage(100, new int[] { 1, 2, 3 }, new { message = "test" });
    }

    [Fact]
    public void LeaveChan_Params_Long_ShouldAcceptLongArray()
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        client.LeaveChan("channel1", 1L, 2L, 3L);
    }

    [Fact]
    public void LeaveChan_Params_Int_ShouldAcceptIntArray()
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        client.LeaveChan("channel1", 1, 2, 3);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(100L)]
    public void ConvertToInt64_Long_RoundTrip_ShouldPreserveValue(long input)
    {
        var client = new ImClient<long>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var convertMethod = typeof(ImClient<long>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var backMethod = typeof(ImClient<long>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var longValue = convertMethod.Invoke(client, [input]);
        var result = backMethod.Invoke(client, [longValue]);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void ConvertToInt64_Int_RoundTrip_ShouldPreserveValue(int input)
    {
        var client = new ImClient<int>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var convertMethod = typeof(ImClient<int>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var backMethod = typeof(ImClient<int>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var longValue = convertMethod.Invoke(client, [input]);
        var result = backMethod.Invoke(client, [longValue]);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData((short)1)]
    [InlineData((short)100)]
    public void ConvertToInt64_Short_RoundTrip_ShouldPreserveValue(short input)
    {
        var client = new ImClient<short>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var convertMethod = typeof(ImClient<short>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var backMethod = typeof(ImClient<short>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var longValue = convertMethod.Invoke(client, [input]);
        var result = backMethod.Invoke(client, [longValue]);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData((byte)1)]
    [InlineData((byte)100)]
    public void ConvertToInt64_Byte_RoundTrip_ShouldPreserveValue(byte input)
    {
        var client = new ImClient<byte>(new ImClientOptions { Redis = _redis, Servers = new[] { "127.0.0.1:6001" } });
        var convertMethod = typeof(ImClient<byte>).GetMethod("ConvertToInt64", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var backMethod = typeof(ImClient<byte>).GetMethod("ConvertToClientIdType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var longValue = convertMethod.Invoke(client, [input]);
        var result = backMethod.Invoke(client, [longValue]);
        Assert.Equal(input, result);
    }
}
