using FreeRedis;

namespace FreeIM.Tests;

/// <summary>
/// ImHelper 非泛型版本单元测试（向后兼容性测试）
/// </summary>
public class ImHelperTests
{
    [Fact]
    public void Initialization_ShouldSetInstance()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        Assert.NotNull(ImHelper.Instance);
    }

    [Fact]
    public void PrevConnectServer_ShouldReturnWebSocketUrl()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" }, PathMatch = "/ws" };
        ImHelper.Initialization(options);
        var url = ImHelper.PrevConnectServer(12345L, "192.168.1.1");
        Assert.Contains("ws://", url);
        Assert.Contains("token=", url);
    }

    [Fact]
    public void SendMessage_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.SendMessage(100L, new long[] { 1, 2, 3 }, new { message = "test" });
    }

    [Fact]
    public void ForceOffline_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.ForceOffline(999999L);
    }

    [Fact]
    public void JoinChan_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.JoinChan(12345L, "channel1");
    }

    [Fact]
    public void LeaveChan_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.LeaveChan(12345L, "channel1");
    }

    [Fact]
    public void LeaveChan_Params_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.LeaveChan("channel1", 1L, 2L, 3L);
    }

    [Fact]
    public void GetChanClientList_ShouldReturnArray()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        var result = ImHelper.GetChanClientList("channel1");
        Assert.IsType<long[]>(result);
    }

    [Fact]
    public void ClearChanClient_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.ClearChanClient("channel1");
    }

    [Fact]
    public void GetChanListByClientId_ShouldReturnArray()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        var result = ImHelper.GetChanListByClientId(12345L);
        Assert.IsType<string[]>(result);
    }

    [Fact]
    public void SendChanMessage_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.SendChanMessage(12345L, "channel1", new { message = "test" });
    }

    [Fact]
    public void SendBroadcastMessage_ShouldNotThrow()
    {
        var options = new ImClientOptions { Redis = new RedisClient("127.0.0.1:6379"), Servers = new[] { "127.0.0.1:6001" } };
        ImHelper.Initialization(options);
        ImHelper.SendBroadcastMessage(new { message = "broadcast" });
    }

    [Fact]
    public void Instance_WithoutInitialization_ShouldThrow()
    {
        typeof(ImHelper).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
        var exception = Assert.Throws<Exception>(() => ImHelper.Instance);
        Assert.Equal("使用前请初始化 ImHelper.Initialization(...);", exception.Message);
    }
}
