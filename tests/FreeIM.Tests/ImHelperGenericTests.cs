using FreeRedis;

namespace FreeIM.Tests;

/// <summary>
/// ImHelper<TClientId> 泛型版本单元测试（不依赖 Redis 的测试）
/// </summary>
public class ImHelperGenericTests
{
    [Fact]
    public void Initialization_Long_ShouldSetInstance()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<long>.Initialization(options);
        Assert.NotNull(ImHelper<long>.Instance);
    }

    [Fact]
    public void Initialization_Int_ShouldSetInstance()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        Assert.NotNull(ImHelper<int>.Instance);
    }

    [Fact]
    public void PrevConnectServer_Long_ShouldReturnWebSocketUrl()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" }, PathMatch = "/ws" };
        ImHelper<long>.Initialization(options);
        var url = ImHelper<long>.PrevConnectServer(12345L, "192.168.1.1");
        Assert.Contains("ws://", url);
        Assert.Contains("token=", url);
    }

    [Fact]
    public void PrevConnectServer_Int_ShouldReturnWebSocketUrl()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" }, PathMatch = "/ws" };
        ImHelper<int>.Initialization(options);
        var url = ImHelper<int>.PrevConnectServer(12345, "192.168.1.1");
        Assert.Contains("ws://", url);
        Assert.Contains("token=", url);
    }

    [Fact]
    public void SendMessage_Long_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<long>.Initialization(options);
        ImHelper<long>.SendMessage(100L, new long[] { 1, 2, 3 }, new { message = "test" });
    }

    [Fact]
    public void SendMessage_Int_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.SendMessage(100, new int[] { 1, 2, 3 }, new { message = "test" });
    }

    [Fact]
    public void ForceOffline_Long_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<long>.Initialization(options);
        ImHelper<long>.ForceOffline(999999L);
    }

    [Fact]
    public void ForceOffline_Int_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.ForceOffline(999999);
    }

    [Fact]
    public void JoinChan_Long_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<long>.Initialization(options);
        ImHelper<long>.JoinChan(12345L, "channel1");
    }

    [Fact]
    public void JoinChan_Int_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.JoinChan(12345, "channel1");
    }

    [Fact]
    public void LeaveChan_Long_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<long>.Initialization(options);
        ImHelper<long>.LeaveChan(12345L, "channel1");
    }

    [Fact]
    public void LeaveChan_Int_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.LeaveChan(12345, "channel1");
    }

    [Fact]
    public void LeaveChan_Params_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.LeaveChan("channel1", 1, 2, 3);
    }

    [Fact]
    public void GetChanClientList_Int_ShouldReturnIntArray()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        var result = ImHelper<int>.GetChanClientList("channel1");
        Assert.IsType<int[]>(result);
    }

    [Fact]
    public void ClearChanClient_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.ClearChanClient("channel1");
    }

    [Fact]
    public void GetChanListByClientId_Int_ShouldReturnArray()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        var result = ImHelper<int>.GetChanListByClientId(12345);
        Assert.IsType<string[]>(result);
    }

    [Fact]
    public void SendChanMessage_Int_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.SendChanMessage(12345, "channel1", new { message = "test" });
    }

    [Fact]
    public void SendBroadcastMessage_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper<int>.Initialization(options);
        ImHelper<int>.SendBroadcastMessage(new { message = "broadcast" });
    }

    [Fact]
    public void Instance_WithoutInitialization_ShouldThrow()
    {
        typeof(ImHelper<int>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
        var exception = Assert.Throws<Exception>(() => ImHelper<int>.Instance);
        Assert.Equal("使用前请初始化 ImHelper<TClientId>.Initialization(...);", exception.Message);
    }
}
