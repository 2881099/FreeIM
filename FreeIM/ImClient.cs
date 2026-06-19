using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// im 核心类实现
/// </summary>
/// <typeparam name="TClientId">客户端ID类型，约束为数值类型（如 long、int、short 等）</typeparam>
public class ImClient<TClientId> where TClientId : struct, IComparable<TClientId>
{
    protected RedisClient _redis;
    protected string[] _servers;
    protected string _redisPrefix;
    protected string _pathMatch;
    protected JsonSerializerSettings _jsonSerializerSettings;

    /// <summary>
    /// 推送消息的事件，可审查推向哪个Server节点
    /// </summary>
    public EventHandler<ImSendEventArgs<TClientId>> OnSend;

    /// <summary>
    /// 初始化 imclient
    /// </summary>
    /// <param name="options"></param>
    public ImClient(ImClientOptions options)
    {
        if (options.Redis == null) throw new ArgumentException("ImClientOptions.Redis 参数不能为空");
        if (options.Servers.Any() == false) throw new ArgumentException("ImClientOptions.Servers 参数不能为空");
        _redis = options.Redis;
        _servers = options.Servers;
        _redisPrefix = $"im_v2{options.PathMatch.Replace('/', '_')}";
        _pathMatch = options.PathMatch ?? "/ws";
        _jsonSerializerSettings = options.JsonSerializerSettings;
    }

    /// <summary>
    /// 负载分区规则：clientId求模
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <returns></returns>
    protected string SelectServer(TClientId clientId)
    {
        // 转换为 long 进行求模运算
        var clientIdLong = ConvertToInt64(clientId);
        var servers_idx = clientIdLong % _servers.Length;
        if (servers_idx >= _servers.Length) servers_idx = 0;
        return _servers[servers_idx];
    }

    /// <summary>
    /// 将泛型 clientId 转换为 long
    /// </summary>
    private long ConvertToInt64(TClientId clientId)
    {
        if (clientId is long l) return l;
        if (clientId is int i) return i;
        if (clientId is short s) return s;
        if (clientId is byte b) return b;
        if (clientId is ulong ul) return (long)ul;
        if (clientId is uint ui) return (long)ui;
        if (clientId is ushort us) return (long)us;
        throw new InvalidOperationException($"不支持的 clientId 类型: {typeof(TClientId)}");
    }

    /// <summary>
    /// ImServer 连接前的负载、授权，返回 ws 目标地址，使用该地址连接 websocket 服务端
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="clientMetaData">客户端相关信息，比如ip</param>
    /// <returns>websocket 地址：ws://xxxx/ws?token=xxx</returns>
    public string PrevConnectServer(TClientId clientId, string clientMetaData)
    {
        var server = SelectServer(clientId);
        var token = $"{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}".Replace("-", "");
        _redis.Set($"{_redisPrefix}Token{token}", JsonConvert.SerializeObject((ConvertToInt64(clientId), clientMetaData), _jsonSerializerSettings), 10);
        return $"ws://{server}{_pathMatch}?token={token}";
    }

    /// <summary>
    /// 向指定的多个客户端id发送消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="receiveClientId">接收者的客户端id</param>
    /// <param name="message">消息</param>
    /// <param name="receipt">是否回执</param>
    public void SendMessage(TClientId senderClientId, IEnumerable<TClientId> receiveClientId, object message, bool receipt = false)
    {
        var receiverArray = receiveClientId.Distinct().ToArray();
        Dictionary<string, ImSendEventArgs<TClientId>> redata = new Dictionary<string, ImSendEventArgs<TClientId>>();

        foreach (var uid in receiverArray)
        {
            string server = SelectServer(uid);
            if (redata.ContainsKey(server) == false) redata.Add(server, new ImSendEventArgs<TClientId>(server, senderClientId, message, receipt));
            redata[server].ReceiveClientId.Add(uid);
        }
        var messageJson = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
        using (var pipe = _redis.StartPipe())
        {
            foreach (var sendArgs in redata.Values)
            {
                OnSend?.Invoke(this, sendArgs);
                var receiverLongs = sendArgs.ReceiveClientId.Select(a => ConvertToInt64(a)).ToArray();
                pipe.Publish($"{_redisPrefix}Server{sendArgs.Server}",
                    JsonConvert.SerializeObject((ConvertToInt64(senderClientId), receiverLongs, messageJson, sendArgs.Receipt), _jsonSerializerSettings));
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 获取所在线客户端id
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TClientId> GetClientListByOnline()
    {
        return _redis.HKeys($"{_redisPrefix}Online")
            .Select(a => long.TryParse(a, out var tryval) ? tryval : 0)
            .Where(a => a != 0)
            .Select(a => ConvertToClientIdType(a));
    }

    /// <summary>
    /// 将 long 转换为泛型 clientId 类型
    /// </summary>
    private TClientId ConvertToClientIdType(long value)
    {
        if (typeof(TClientId) == typeof(long)) return (TClientId)(object)value;
        if (typeof(TClientId) == typeof(int)) return (TClientId)(object)(int)value;
        if (typeof(TClientId) == typeof(short)) return (TClientId)(object)(short)value;
        if (typeof(TClientId) == typeof(byte)) return (TClientId)(object)(byte)value;
        throw new InvalidOperationException($"不支持的 clientId 类型: {typeof(TClientId)}");
    }

    /// <summary>
    /// 判断客户端是否在线
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public bool HasOnline(TClientId clientId)
    {
        return _redis.HGet<long>($"{_redisPrefix}Online", clientId.ToString()) > 0;
    }

    /// <summary>
    /// 判断客户端是否在线
    /// </summary>
    /// <param name="clientIds"></param>
    /// <returns></returns>
    public bool[] HasOnline(IEnumerable<TClientId> clientIds)
    {
        if (clientIds?.Any() != true) return new bool[0];
        return _redis.HMGet<long>($"{_redisPrefix}Online", clientIds.Select(a => a.ToString()).ToArray()).Select(a => a > 0).ToArray();
    }

    /// <summary>
    /// 强制下线
    /// </summary>
    /// <param name="clientId"></param>
    public void ForceOffline(TClientId clientId)
    {
        string server = SelectServer(clientId);
        _redis.Publish($"{_redisPrefix}Server{server}", $"__FreeIM__(ForceOffline){ConvertToInt64(clientId)}");
    }

    /// <summary>
    /// 事件订阅
    /// </summary>
    /// <param name="online">上线</param>
    /// <param name="offline">下线</param>
    public void EventBus(
        Action<(TClientId clientId, string clientMetaData)> online,
        Action<(TClientId clientId, string clientMetaData)> offline)
    {
        var chanOnline = $"evt_{_redisPrefix}Online";
        var chanOffline = $"evt_{_redisPrefix}Offline";
        _redis.Subscribe(new[] { chanOnline, chanOffline }, (chan, msg) =>
        {
            if (chan == chanOnline)
            {
                var data = JsonConvert.DeserializeObject<(long clientId, string clientMetaData)>(msg as string);
                online((ConvertToClientIdType(data.clientId), data.clientMetaData));
            }
            if (chan == chanOffline)
            {
                var data = JsonConvert.DeserializeObject<(long clientId, string clientMetaData)>(msg as string);
                offline((ConvertToClientIdType(data.clientId), data.clientMetaData));
            }
        });
    }

    #region 群聊频道，每次上线都必须重新加入

    /// <summary>
    /// 加入群聊频道，每次上线都必须重新加入
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chans">群聊频道名</param>
    public void JoinChan(TClientId clientId, params string[] chans)
    {
        if (chans?.Any() != true) return;
        var clientIdStr = ConvertToInt64(clientId).ToString();
        using (var pipe = _redis.StartPipe())
        {
            foreach (var chan in chans)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                pipe.Eval($"if redis.call('HSETNX',KEYS[1],ARGV[1],0)==1 then redis.call('HSET',KEYS[2],ARGV[2],0) redis.call('HINCRBY',KEYS[3],ARGV[2],1) end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientIdStr}", $"{_redisPrefix}ListChan" }, new object[] { clientIdStr, chan });
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 离开群聊频道
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chans">群聊频道名</param>
    public void LeaveChan(TClientId clientId, params string[] chans)
    {
        if (chans?.Any() != true) return;
        var clientIdStr = ConvertToInt64(clientId).ToString();
        using (var pipe = _redis.StartPipe())
        {
            foreach (var chan in chans)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                pipe.Eval($"if redis.call('HDEL',KEYS[1],ARGV[1])==1 then redis.call('HDEL',KEYS[2],ARGV[2]) if redis.call('HINCRBY',KEYS[3],ARGV[2],-1)<=0 then redis.call('HDEL',KEYS[3],ARGV[2]) end end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientIdStr}", $"{_redisPrefix}ListChan" }, new object[] { clientIdStr, chan });
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 离开群聊频道
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <param name="clientIds">客户端id</param>
    public void LeaveChan(string chan, params TClientId[] clientIds)
    {
        if (string.IsNullOrEmpty(chan)) return;
        if (clientIds?.Any() != true) return;
        using (var pipe = _redis.StartPipe())
        {
            foreach (var clientId in clientIds)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                var clientIdStr = ConvertToInt64(clientId).ToString();
                pipe.Eval($"if redis.call('HDEL',KEYS[1],ARGV[1])==1 then redis.call('HDEL',KEYS[2],ARGV[2]) if redis.call('HINCRBY',KEYS[3],ARGV[2],-1)<=0 then redis.call('HDEL',KEYS[3],ARGV[2]) end end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientIdStr}", $"{_redisPrefix}ListChan" }, new object[] { clientIdStr, chan });
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 获取群聊频道所有客户端id（测试）
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <returns></returns>
    public TClientId[] GetChanClientList(string chan)
    {
        if (string.IsNullOrEmpty(chan)) return new TClientId[0];
        return _redis.HKeys($"{_redisPrefix}Chan{chan}")
            .Select(a => long.TryParse(a, out var tryval) ? tryval : 0)
            .Where(a => a != 0)
            .Select(a => ConvertToClientIdType(a))
            .ToArray();
    }

    /// <summary>
    /// 清理群聊频道的离线客户端（测试）
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    public void ClearChanClient(string chan)
    {
        if (string.IsNullOrEmpty(chan)) return;
        var websocketIds = _redis.HKeys($"{_redisPrefix}Chan{chan}");
        var offline = new List<string>();
        var span = websocketIds.AsSpan();
        var start = span.Length;
        while (start > 0)
        {
            start = start - 10;
            var length = 10;
            if (start < 0)
            {
                length = start + 10;
                start = 0;
            }
            var slice = span.Slice(start, length);
            var hvals = _redis.HMGet($"{_redisPrefix}Online", slice.ToArray().Select(b => b.ToString()).ToArray());
            for (var a = length - 1; a >= 0; a--)
            {
                if (string.IsNullOrEmpty(hvals[a]))
                {
                    offline.Add(span[start + a]);
                    span[start + a] = null;
                }
            }
        }
        //删除离线订阅
        if (offline.Any()) _redis.HDel($"{_redisPrefix}Chan{chan}", offline.ToArray());
    }

    /// <summary>
    /// 获取所有群聊频道和在线人数
    /// </summary>
    /// <returns>频道名和在线人数</returns>
    public IEnumerable<(string chan, long online)> GetChanList()
    {
        var ret = _redis.HGetAll<long>($"{_redisPrefix}ListChan");
        return ret.Select(a => (a.Key, a.Value));
    }

    /// <summary>
    /// 获取用户参与的所有群聊频道
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <returns></returns>
    public string[] GetChanListByClientId(TClientId clientId)
    {
        return _redis.HKeys($"{_redisPrefix}Client{ConvertToInt64(clientId)}");
    }

    /// <summary>
    /// 获取群聊频道的在线人数
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <returns>在线人数</returns>
    public long GetChanOnline(string chan)
    {
        if (string.IsNullOrEmpty(chan)) return 0;
        return _redis.HGet<long>($"{_redisPrefix}ListChan", chan);
    }

    /// <summary>
    /// 发送群聊消息，在线的用户将收到消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="chan">群聊频道名</param>
    /// <param name="message">消息</param>
    public void SendChanMessage(TClientId senderClientId, string chan, object message)
    {
        var sendArgs = _servers.Select(server => new ImSendEventArgs<TClientId>(server, senderClientId, message, false) { Chan = chan }).ToArray();
        var messageJson = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
        using (var pipe = _redis.StartPipe())
        {
            foreach (var arg in sendArgs)
            {
                OnSend?.Invoke(this, arg);
                pipe.Publish($"{_redisPrefix}Server{arg.Server}", $"__FreeIM__(ChanMessage){JsonConvert.SerializeObject((ConvertToInt64(arg.SenderClientId), chan, messageJson), _jsonSerializerSettings)}");
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 发送广播消息
    /// </summary>
    /// <param name="message">消息</param>
    public void SendBroadcastMessage(object message) => SendChanMessage(default, null, message);
    #endregion
}

/// <summary>
/// 兼容旧版本的 ImClient 类型别名（使用 long 作为 clientId）
/// </summary>
public class ImClient : ImClient<long>
{
    /// <summary>
    /// 使用默认配置初始化 ImClient
    /// </summary>
    /// <param name="options"></param>
    public ImClient(ImClientOptions options) : base(options) { }
}
