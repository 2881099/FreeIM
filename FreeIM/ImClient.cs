using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// im 核心类实现
/// </summary>
public class ImClient
{
    protected RedisClient _redis;
    protected string[] _servers;
    protected string _redisPrefix;
    protected string _pathMatch;

    /// <summary>
    /// 推送消息的事件，可审查推向哪个Server节点
    /// </summary>
    public EventHandler<ImSendEventArgs> OnSend;

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
    }

    /// <summary>
    /// 负载分区规则：clientId求模
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <returns></returns>
    protected string SelectServer(long clientId)
    {
        var servers_idx = clientId % _servers.Length;
        if (servers_idx >= _servers.Length) servers_idx = 0;
        return _servers[servers_idx];
    }

    /// <summary>
    /// ImServer 连接前的负载、授权，返回 ws 目标地址，使用该地址连接 websocket 服务端
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="clientMetaData">客户端相关信息，比如ip</param>
    /// <returns>websocket 地址：ws://xxxx/ws?token=xxx</returns>
    public string PrevConnectServer(long clientId, string clientMetaData)
    {
        var server = SelectServer(clientId);
        var token = $"{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}".Replace("-", "");
        _redis.Set($"{_redisPrefix}Token{token}", JsonConvert.SerializeObject((clientId, clientMetaData)), 10);
        return $"ws://{server}{_pathMatch}?token={token}";
    }

    /// <summary>
    /// 向指定的多个客户端id发送消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="receiveClientId">接收者的客户端id</param>
    /// <param name="message">消息</param>
    /// <param name="receipt">是否回执</param>
    public void SendMessage(long senderClientId, IEnumerable<long> receiveClientId, object message, bool receipt = false)
    {
        receiveClientId = receiveClientId.Distinct().ToArray();
        Dictionary<string, ImSendEventArgs> redata = new Dictionary<string, ImSendEventArgs>();

        foreach (var uid in receiveClientId)
        {
            string server = SelectServer(uid);
            if (redata.ContainsKey(server) == false) redata.Add(server, new ImSendEventArgs(server, senderClientId, message, receipt));
            redata[server].ReceiveClientId.Add(uid);
        }
        var messageJson = JsonConvert.SerializeObject(message);
        using (var pipe = _redis.StartPipe())
        {
            foreach (var sendArgs in redata.Values)
            {
                OnSend?.Invoke(this, sendArgs);
                pipe.Publish($"{_redisPrefix}Server{sendArgs.Server}",
                    JsonConvert.SerializeObject((senderClientId, sendArgs.ReceiveClientId, messageJson, sendArgs.Receipt)));
            }
            pipe.EndPipe();
        }
    }

    /// <summary>
    /// 获取所在线客户端id
    /// </summary>
    /// <returns></returns>
    public IEnumerable<long> GetClientListByOnline()
    {
        return _redis.HKeys($"{_redisPrefix}Online").Select(a => long.TryParse(a, out var tryval) ? tryval : 0).Where(a => a != 0);
    }

    /// <summary>
    /// 判断客户端是否在线
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public bool HasOnline(long clientId)
    {
        return _redis.HGet<long>($"{_redisPrefix}Online", clientId.ToString()) > 0;
    }
    /// <summary>
    /// 判断客户端是否在线
    /// </summary>
    /// <param name="clientIds"></param>
    /// <returns></returns>
    public bool[] HasOnline(IEnumerable<long> clientIds)
    {
        if (clientIds?.Any() != true) return new bool[0];
        return _redis.HMGet<long>($"{_redisPrefix}Online", clientIds.Select(a => a.ToString()).ToArray()).Select(a => a > 0).ToArray();
    }

    /// <summary>
    /// 强制下线
    /// </summary>
    /// <param name="clientId"></param>
    public void ForceOffline(long clientId)
    {
        string server = SelectServer(clientId);
        _redis.Publish($"{_redisPrefix}Server{server}", $"__FreeIM__(ForceOffline){clientId}");
    }

    /// <summary>
    /// 事件订阅
    /// </summary>
    /// <param name="online">上线</param>
    /// <param name="offline">下线</param>
    public void EventBus(
        Action<(long clientId, string clientMetaData)> online,
        Action<(long clientId, string clientMetaData)> offline)
    {
        var chanOnline = $"evt_{_redisPrefix}Online";
        var chanOffline = $"evt_{_redisPrefix}Offline";
        _redis.Subscribe(new[] { chanOnline, chanOffline }, (chan, msg) =>
        {
            if (chan == chanOnline) online(JsonConvert.DeserializeObject<(long clientId, string clientMetaData)>(msg as string));
            if (chan == chanOffline) offline(JsonConvert.DeserializeObject<(long clientId, string clientMetaData)>(msg as string));
        });
    }

    #region 群聊频道，每次上线都必须重新加入

    /// <summary>
    /// 加入群聊频道，每次上线都必须重新加入
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chans">群聊频道名</param>
    public void JoinChan(long clientId, params string[] chans)
    {
        if (chans?.Any() != true) return;
        using (var pipe = _redis.StartPipe())
        {
            foreach (var chan in chans)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                pipe.Eval($"if redis.call('HSETNX',KEYS[1],ARGV[1],0)==1 then redis.call('HSET',KEYS[2],ARGV[2],0) redis.call('HINCRBY',KEYS[3],ARGV[2],1) end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientId}", $"{_redisPrefix}ListChan" }, new object[] { clientId, chan });
                //pipe.HSet($"{_redisPrefix}Chan{chan}", clientId.ToString(), 0);
                //pipe.HSet($"{_redisPrefix}Client{clientId}", chan, 0);
                //pipe.HIncrBy($"{_redisPrefix}ListChan", chan, 1);
            }
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 离开群聊频道
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chans">群聊频道名</param>
    public void LeaveChan(long clientId, params string[] chans)
    {
        if (chans?.Any() != true) return;
        using (var pipe = _redis.StartPipe())
        {
            foreach (var chan in chans)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                pipe.Eval($"if redis.call('HDEL',KEYS[1],ARGV[1])==1 then redis.call('HDEL',KEYS[2],ARGV[2]) if redis.call('HINCRBY',KEYS[3],ARGV[2],-1)<=0 then redis.call('HDEL',KEYS[3],ARGV[2]) end end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientId}", $"{_redisPrefix}ListChan" }, new object[] { clientId, chan });
                //pipe.HDel($"{_redisPrefix}Chan{chan}", clientId.ToString());
                //pipe.HDel($"{_redisPrefix}Client{clientId}", chan);
                //pipe.Eval($"if redis.call('HINCRBY', KEYS[1], '{chan}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{chan}') end return 1", new[] { $"{_redisPrefix}ListChan" });
            }
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 离开群聊频道
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <param name="clientIds">客户端id</param>
    public void LeaveChan(string chan, params long[] clientIds)
    {
        if (string.IsNullOrEmpty(chan)) return;
        if (clientIds?.Any() != true) return;
        using (var pipe = _redis.StartPipe())
        {
            foreach (var clientId in clientIds)
            {
                if (string.IsNullOrEmpty(chan)) continue;
                pipe.Eval($"if redis.call('HDEL',KEYS[1],ARGV[1])==1 then redis.call('HDEL',KEYS[2],ARGV[2]) if redis.call('HINCRBY',KEYS[3],ARGV[2],-1)<=0 then redis.call('HDEL',KEYS[3],ARGV[2]) end end return 1",
                    new[] { $"{_redisPrefix}Chan{chan}", $"{_redisPrefix}Client{clientId}", $"{_redisPrefix}ListChan" }, new object[] { clientId, chan });
                //pipe.HDel($"{_redisPrefix}Chan{chan}", clientId.ToString());
                //pipe.HDel($"{_redisPrefix}Client{clientId}", chan);
                //pipe.Eval($"if redis.call('HINCRBY', KEYS[1], '{chan}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{chan}') end return 1", new[] { $"{_redisPrefix}ListChan" });
            }
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 获取群聊频道所有客户端id（测试）
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <returns></returns>
    public long[] GetChanClientList(string chan)
    {
        if (string.IsNullOrEmpty(chan)) return new long[0];
        return _redis.HKeys($"{_redisPrefix}Chan{chan}").Select(a => long.TryParse(a, out var tryval) ? tryval : 0).Where(a => a != 0).ToArray();
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
    public string[] GetChanListByClientId(long clientId)
    {
        return _redis.HKeys($"{_redisPrefix}Client{clientId}");
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
    public void SendChanMessage(long senderClientId, string chan, object message)
    {
        var sendArgs = _servers.Select(server => new ImSendEventArgs(server, senderClientId, message, false) { Chan = chan }).ToArray();
        var messageJson = JsonConvert.SerializeObject(message);
        using (var pipe = _redis.StartPipe())
        {
            foreach (var arg in sendArgs)
            {
                OnSend?.Invoke(this, arg);
                pipe.Publish($"{_redisPrefix}Server{arg.Server}", $"__FreeIM__(ChanMessage){JsonConvert.SerializeObject((senderClientId, chan, messageJson))}");
            }
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 发送广播消息
    /// </summary>
    /// <param name="message">消息</param>
    public void SendBroadcastMessage(object message) => SendChanMessage(0, null, message);
    #endregion
}
