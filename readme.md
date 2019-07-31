利用 WebSocket 实现简易、高性能、集群即时通讯组件，支持点对点通讯、群聊通讯、上线下线事件消息等众多实用性功能。

# Quick Start

> dotnet add package ImCore

## IM服务端
```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseImServer(new ImServerOptions
    {
        Redis = new CSRedis.CSRedisClient("127.0.0.1:6379,poolsize=5"),
        Servers = new[] { "127.0.0.1:6001" }, //集群配置
        Server = "127.0.0.1:6001"
    });
}
```
> 一套永远不需要迭代更新的IM服务端

## WebApi业务端
```csharp
public void Configure(IApplicationBuilder app)
{
    //...

    ImHelper.Initialization(new ImClientOptions
    {
        Redis = new CSRedis.CSRedisClient("127.0.0.1:6379,poolsize=5"),
        Servers = new[] { "127.0.0.1:6001" }
    });

    ImHelper.EventBus(
        t => Console.WriteLine(t.clientId + "上线了"), 
        t => Console.WriteLine(t.clientId + "下线了"));
}
```

| ImHelper方法 | 参数 | 描述 |
| - | - | - |
| PrevConnectServer | (clientId, string) | 在终端准备连接 WebSocket 前调用 |
| SendMessage | (发送者, 接收者, 消息内容, 是否回执) | 发送消息 |
| GetClientListByOnline | - | 返回所有在线clientId |
| EventBus | (上线委托, 离线委托) | socket上线与下线事件 |


| 群聊频道 | 参数 | 描述 |
| - | - | - |
| JoinChan | (clientId, 频道名) | 加入 |
| LeaveChan | (clientId, 频道名) | 离开 |
| GetChanClientList | (频道名) | 获取群聊频道所有clientId |
| GetChanList | - | 获取所有群聊频道和在线人数 |
| GetChanListByClientId | (clientId) | 获取用户参与的所有群聊频道 |
| GetChanOnline | (频道名) | 获取群聊频道的在线人数 |
| SendChanMessage | (clientId, 频道名, 消息内容) | 发送群聊消息，所有在线的用户将收到消息 |

说明：clientId 应该与 webApi的用户id相同，或者有关联。

## Html5终端

本方案支持集群分区，前端连接 websocket 前，应该先请求 webApi 获得地址(ImHelper.PrevConnectServer)。

# 运行示例

> 运行环境：.NETCore 2.1 + redis-server 2.8

> [下载Redis-x64-2.8.2402.zip](https://files.cnblogs.com/files/kellynic/Redis-x64-2.8.2402.zip)，点击 start.bat 运行；

> cd imServer && dotnet run

> cd web && dotnet run

> 打开多个浏览器，访问 http://127.0.0.1:5000 发送群消息

![image](https://user-images.githubusercontent.com/16286519/62152387-05980c00-b335-11e9-8b6d-3f6d03bb3629.png)

# 设计思路

imServer 是 websocket 服务中心，可部署多实例，按clientId分区管理socket连接；

webApi 或其他应用端，使用 ImHelper 调用相关方法（如：SendMessage、群聊相关方法）；

消息发送利用了 redis 订阅发布技术。每个 imServer 订阅相应的频道，收到消息，指派 websocket 向终端（如浏览器）发送消息；

1、可缓解并发推送消息过多的问题；

2、可解决连接数过多的问题；

客户端连接流程：client -> websocket -> imserver

imserver 订阅消息：client <- imserver <- redis channel

推送消息流程：web1 -> sendmsg方法 -> redis channel -> imserver

imserver 充当消息转发，及维护连接中心，代码万年不变不需要重启维护；

## WebSocket

比较笨的办法是浏览器端使用websocket，其他端socket，这种混乱的设计非常难维护。

强烈建议所有端都使用websocket协议，websocket协议支持几乎所有端，adorid/ios/h5/小程序全部支持websocket客户端。

websocket用了后，就像跨平台。。。虽然选一种语言都能连接通讯。

## 业务与通讯协议

im系统一般涉及【我的好友】、【我的群】、【历史消息】等等。。

那么，imServer与业务方(webApi)该保持何种关系呢？

用户A向好友B发送消息，分析一下：

* 需要判断B是否为A好友；
* 需要判断A是否有权限；
* 等等。。

诸如此类业务判断会很复杂，我们试想一下，如果使用imServer做业务协议，它是不是会变成巨无霸难以维护？

又比如获取历史聊天记录，难道客户端要先websocket.send('gethistory')，再在onmessage里定位回调处理？

---

我们可以这样设定，所有用户的主动行为走业务方(webApi)，imServer只负责即时消息推送。什么意思？

用户A向好友B发送消息：客户端请求业务方(webApi)接口，由业务方(webApi)后端向imServer发起推送请求，imServer收到指令后，向前端用户B的websocket发送数据，用户B收到了消息。

获取历史消息：客户端请求业务方(webApi)接口，返回json(历史消息)

回执：用户A如何知道消息发送状态（成功或失败或不在线）？imServer端向用户B发送消息时，把状态以消息的方式推给用户A即可（按上面的逻辑），具体请看源码吧。。。

## 发送消息

采用 redis 轻量级的订阅发布功能，实现消息缓冲发送。

## 集群分区

单个imServer实例支持多少个客户端连接，两千个没问题？

如果在线用户有10万人，怎么办？？？

比如部署4个imServer：

imServer1 订阅 redisChanne1
imServer2 订阅 redisChanne2
imServer3 订阅 redisChanne3
imServer4 订阅 redisChanne4

业务方(webApi)端根据接收方的clientId后四位16进制与节点总数取模，定位到对应的redisChannel，进行redis->publish操作将消息定位到相应的imServer。

每个 imServer 管理着对应的终端连接，当接收到 redis 订阅消息后，向对应的终端连接推送数据。

## 事件消息

IM 系统比较常用的有上线、下线，在 imServer 层才能准确捕捉事件，但业务代码就不合适在这上面编写了。

采用 redis 发布订阅技术，将上线、下线等事件向指定频道发布，业务方(webApi) 通过 ImHelper.EventBus 方法进行订阅捕捉。

![image](https://user-images.githubusercontent.com/16286519/62150466-a46e3980-b330-11e9-86f3-d050160f0913.png)