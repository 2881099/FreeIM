using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// im 核心类实现的配置所需
/// </summary>
public class ImClientOptions
{
    /// <summary>
    /// 用于存储数据和发送消息
    /// </summary>
    public RedisClient Redis { get; set; }
    /// <summary>
    /// 负载的服务端
    /// </summary>
    public string[] Servers { get; set; }
    /// <summary>
    /// websocket请求的路径，默认值：/ws
    /// </summary>
    public string PathMatch { get; set; } = "/ws";
    /// <summary>
    /// Json 序列化设置
    /// </summary>
    public JsonSerializerSettings JsonSerializerSettings { get;set;}
}

public class ImSendEventArgs : EventArgs
{
    /// <summary>
    /// 发送者的客户端id
    /// </summary>
    public long SenderClientId { get; }
    /// <summary>
    /// 接收者的客户端id
    /// </summary>
    public List<long> ReceiveClientId { get; } = new List<long>();
    public string Chan { get; internal set; }
    /// <summary>
    /// imServer 服务器节点
    /// </summary>
    public string Server { get; }
    /// <summary>
    /// 消息
    /// </summary>
    public object Message { get; }
    /// <summary>
    /// 是否回执
    /// </summary>
    public bool Receipt { get; }

    internal ImSendEventArgs(string server, long senderClientId, object message, bool receipt = false)
    {
        this.Server = server;
        this.SenderClientId = senderClientId;
        this.Message = message;
        this.Receipt = receipt;
    }
}