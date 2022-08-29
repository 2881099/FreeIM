FreeIM 使用 websocket 协议实现简易、高性能（单机支持5万+连接）、集群即时通讯组件，支持点对点通讯、群聊通讯、上线下线事件消息等众多实用性功能。 `ImCore` 已正式改名为 `FreeIM`。[【网络版斗地主示例】](https://github.com/2881099/FightLandlord)

使用场景：好友聊天、群聊天、直播间、实时评论区、游戏

> dotnet add package FreeIM

### ImServer 服务端

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseFreeImServer(new ImServerOptions
    {
        Redis = new FreeRedis.RedisClient("127.0.0.1:6379,poolsize=5"),
        Servers = new[] { "127.0.0.1:6001" }, //集群配置
        Server = "127.0.0.1:6001"
    });
}
```
> 一套永远不需要迭代更新的 `ImServer` 服务端，支持 .NET6.0、.NETCore2.1+、NETStandard2.0

### WebApi 业务端

```csharp
public void Configure(IApplicationBuilder app)
{
    //...

    ImHelper.Initialization(new ImClientOptions
    {
        Redis = new FreeRedis.RedisClient("127.0.0.1:6379,poolsize=5"),
        Servers = new[] { "127.0.0.1:6001" }
    });

    ImHelper.EventBus(
        t => Console.WriteLine(t.clientId + "上线了"), 
        t => Console.WriteLine(t.clientId + "下线了"));
}
```

| ImHelper方法 | 参数 | 描述 |
| - | - | - |
| PrevConnectServer | (clientId, string) | 在终端准备连接 websocket 前调用 |
| SendMessage | (发送者, 接收者, 消息内容, 是否回执) | 发送消息 |
| GetClientListByOnline | - | 返回所有在线clientId |
| HasOnline | clientId | 判断客户端是否在线 |
| EventBus | (上线委托, 离线委托) | socket上线与下线事件 |


| 频道 | 参数 | 描述 |
| - | - | - |
| JoinChan | (clientId, 频道名) | 加入 |
| LeaveChan | (clientId, 频道名) | 离开 |
| GetChanClientList | (频道名) | 获取频道所有clientId |
| GetChanList | - | 获取所有频道和在线人数 |
| GetChanListByClientId | (clientId) | 获取用户参与的所有频道 |
| GetChanOnline | (频道名) | 获取频道的在线人数 |
| SendChanMessage | (clientId, 频道名, 消息内容) | 发送消息，所有在线的用户将收到消息 |

- clientId 应该与用户id相同，或者关联；
- 频道适用临时的群聊需求，如聊天室、讨论区；

> ImHelper 支持 .NetFramework 4.5+、.NetStandard 2.0

### Html5 终端

终端连接 websocket 前，应该先请求 `WebApi` 获得授权过的地址(ImHelper.PrevConnectServer)，伪代码：

```javascript
ajax('/prev-connect-imserver', function(data) {
    var url = data; //此时的值：ws://127.0.0.1:6001/ws?token=xxxxx
    var sock = new WebSocket(url);
    sock.onmessage = function (e) {
        //...
    };
})
```

# 项目演示

> 运行环境：.NET6.0 + redis-server 2.8

> cd ImServer && dotnet run --urls=http://*:6001

> cd WebApi && dotnet run

> 打开多个浏览器，分别访问 http://127.0.0.1:5000 发送群消息

![image](https://user-images.githubusercontent.com/16286519/62152387-05980c00-b335-11e9-8b6d-3f6d03bb3629.png)

# 设计思路

`终端`（如浏览器） 使用 websocket 连接 `ImServer`；

`ImServer` 根据 clientId 分区管理 websocket 连接，`ImServer` 支持群集部署；

`WebApi` 或其他应用端，使用 ImHelper 调用相关方法（如：SendMessage、群聊相关方法），将数据推至 Redis channel；

`ImServer` 订阅 Redis channel，收到消息后向 `终端`（如浏览器）推送消息；

1、缓解了并发推送消息过多的问题；

2、解决了连接数过多的问题；

3、解耦了业务和通讯，架构更加清淅；

- `ImServer` 充当消息转发，连接维护，代码万年不变、且不需要重启维护
- `WebApi` 负责所有业务

解决了协议痛点：如果浏览器使用 websocket 协议，iOS 使用其他协议，协议不一致将很难维护。

> 建议所有端都使用 websocket 协议，adorid/iOS/h5/小程序 全部支持 websocket 客户端。

解决了职责痛点：IM 的系统一般涉及【我的好友】、【我的群】、【历史消息】等等。。那么，`ImServer` 与 `WebApi`(业务方) 该保持何种关系呢？

用户A向好友B发送消息，分析一下：

* 需要判断B是否为A好友；
* 需要判断A是否有权限；

获取历史聊天记录，如果多个 `终端` websocket.send('gethistory')，再在 onmessage 里定位回调处理，将多么麻烦啊？

诸如此类业务判断会很复杂，使用 `ImServer` 做业务逻辑，最终 `ImServer` 和 `终端` 都将变成巨无霸难以维护。

### 发送消息

业务和推送分离的设计，即 `ImServer` 只负责推送工作，`WebApi` 负责业务。

用户A向B发送消息：`终端`A ajax -> `WebApi` -> `ImServer` -> `终端`B websocket.onmessage；

获取历史聊天记录：`终端` 请求 `WebApi`(业务方) 接口，返回json(历史消息)。

FreeIM 强依赖 redis-server 组件功能：

- 集成了 redis 轻量级的订阅发布功能，实现消息缓冲发送，后期可更换为其他技术
- 使用了 redis 存储一些关系数据，如在线 clientId、频道信息、授权信息等

### 集群分区

单个 `ImServer` 实例支持多少个客户端连接，3万人没问题？如果在线用户有10万人，怎么办？？？

部署 4 个 `ImServer`：

`ImServer`1 订阅 redisChanne1

`ImServer`2 订阅 redisChanne2

`ImServer`3 订阅 redisChanne3

`ImServer`4 订阅 redisChanne4

`WebApi`(业务方) 根据接收方的 clientId 后四位 16 进制与节点总数取模，定位到对应的 redisChannel，进行 redis->publish 操作将消息定位到相应的 `ImServer`。

每个 `ImServer` 管理着对应的终端连接，当接收到 redis 订阅消息后，向对应的终端连接推送数据。

### 事件消息

IM 系统比较常用的有上线、下线，在 `ImServer` 层才能准确捕捉事件，但业务代码不合适在这上面编写了。

此时采用 redis 发布订阅，将上线、下线等事件向指定频道发布，`WebApi`(业务方) 通过 ImHelper.EventBus 方法进行订阅捕捉。

![image](https://user-images.githubusercontent.com/16286519/62150466-a46e3980-b330-11e9-86f3-d050160f0913.png)

### A向B发文件的例子

1、A向 `WebApi` 传文件

2、`WebApi` 告诉 `ImServer`，A向B正在传文件，ImHelper.SendMessage(B, "A正在给传送文件...")

3、B收到消息，A正在给传送文件...

4、`WebApi` 文件接收完成时告诉 `ImServer`，A向B文件传输完毕，ImHelper.SendMessage(B, "A文件传输完毕（含文件链接）")

5、B收到消息，A文件传输完毕（含文件链接）

# 有感而发

为什么说 SignalR 不合适做 IM？

1、IM 的特点必定是长连接，轮训的功能用不上；

2、因为 SignalR 是双工通讯的设计，`终端` 使用 hub.invoke 发送命令给SignalR 服务端处理业务，适合用来代替 ajax 减少 http 请求数量；

3、过多使用 hub，SignalR 服务端会被业务入侵，业务变化频繁后不得不重新发布版本，每次部署所有终端都会断开连接，遇到5分钟发一次业务补丁的时候，类似离线和上线提示好友的功能就无法实现；

FreeIM 业务和推送分离设计，`终端` 连接永不更新重启 `ImServer` ，业务代码全部在 `WebApi` 编写，因此重启 `WebApi` 不会造成连接断开。
