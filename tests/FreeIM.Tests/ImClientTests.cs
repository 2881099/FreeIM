using FreeRedis;
using Newtonsoft.Json;

namespace FreeIM.Tests;

/// <summary>
/// ImClient<TClientId> 泛型类单元测试（不依赖 Redis 的测试）
/// </summary>
public class ImClientTests
{
    /// <summary>
    /// 测试 ImClient<long> 构造函数
    /// </summary>
    [Fact]
    public void Constructor_Long_ClientId_ShouldSuccess()
    {
        var options = new ImClientOptions
        {
            Redis = new RedisClient("127.0.0.1:6379"),
            Servers = new[] { "127.0.0.1:6001" }
        };

        var client = new ImClient(options);
        Assert.NotNull(client);
    }

    /// <summary>
    /// 测试 ImClient<int> 构造函数
    /// </summary>
    [Fact]
    public void Constructor_Int_ClientId_ShouldSuccess()
    {
        var options = new ImClientOptions
        {
            Redis = new RedisClient("127.0.0.1:6379"),
            Servers = new[] { "127.0.0.1:6001" }
        };

        var client = new ImClient<int>(options);
        Assert.NotNull(client);
    }

    /// <summary>
    /// 测试 ImClient<short> 构造函数
    /// </summary>
    [Fact]
    public void Constructor_Short_ClientId_ShouldSuccess()
    {
        var options = new ImClientOptions
        {
            Redis = new RedisClient("127.0.0.1:6379"),
            Servers = new[] { "127.0.0.1:6001" }
        };

        var client = new ImClient<short>(options);
        Assert.NotNull(client);
    }

    /// <summary>
    /// 测试 ImClient<byte> 构造函数
    /// </summary>
    [Fact]
    public void Constructor_Byte_ClientId_ShouldSuccess()
    {
        var options = new ImClientOptions
        {
            Redis = new RedisClient("127.0.0.1:6379"),
            Servers = new[] { "127.0.0.1:6001" }
        };

        var client = new ImClient<byte>(options);
        Assert.NotNull(client);
    }

    /// <summary>
    /// 测试构造函数参数验证 - 缺少 Redis
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWithoutRedis()
    {
        var options = new ImClientOptions
        {
            Redis = null!,
            Servers = new[] { "127.0.0.1:6001" }
        };

        var exception = Assert.Throws<ArgumentException>(() => new ImClient(options));
        Assert.Equal("ImClientOptions.Redis 参数不能为空", exception.Message);
    }

    /// <summary>
    /// 测试构造函数参数验证 - 缺少 Servers
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowWithoutServers()
    {
        var options = new ImClientOptions
        {
            Redis = new RedisClient("127.0.0.1:6379"),
            Servers = Array.Empty<string>()
        };

        var exception = Assert.Throws<ArgumentException>(() => new ImClient(options));
        Assert.Equal("ImClientOptions.Servers 参数不能为空", exception.Message);
    }
}
