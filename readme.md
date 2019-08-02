ImCore 利用 webSocket 协议实现简易、高性能、集群即时通讯组件，支持点对点通讯、群聊通讯、上线下线事件消息等众多实用性功能。

# Quick Start

> dotnet add package ImCore

### IM服务端
```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseImServer(new imServerOptions
    {
        Redis = new CSRedis.CSRedisClient("127.0.0.1:6379,poolsize=5"),
        Servers = new[] { "127.0.0.1:6001" }, //集群配置
        Server = "127.0.0.1:6001"
    });
}
```
> 一套永远不需要迭代更新的IM服务端，ImServer 支持 .NetStandard 2.0

### Docker
```shell
docker run \
-e "ImServerOption:Servers=118.25.209.177:6000;118.25.209.177:6001;118.25.209.177:6002" \
-e "ImServerOption:Server=118.25.209.177:6000" \
-e "ImServerOption:CSRedisClient=118.25.209.177:26379,poolsize=5" \
-e "ASPNETCORE_URLS=http://+:6002" \
-p 6002:6002 \
-d \
imserver
```

### WebApi业务端
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
| PrevConnectServer | (clientId, string) | 在终端准备连接 webSocket 前调用 |
| SendMessage | (发送者, 接收者, 消息内容, 是否回执) | 发送消息 |
| GetClientListByOnline | - | 返回所有在线clientId |
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

- clientId 应该与 webApi 的用户id相同，或者有关联；
- 频道适用临时的群聊需求，如：聊天室、即时讨论区；

> ImHelper 支持 .NetFramework 4.5+、.NetStandard 2.0

### Html5终端

前端连接 webSocket 前，应该先请求 webApi 获得授权过的地址(ImHelper.PrevConnectServer)，伪代码：
```javascript
ajax('/prev-connect-imserver', function(data) {
    var url = data; //此时的值：ws://127.0.0.1:6001/ws?token=xxxxx
    var sock = new WebSocket(url);
    sock.onmessage = function (e) {
        //...
    };
})
```

# Demo

> 运行环境：.NETCore 2.1 + redis-server 2.8

> [下载Redis-x64-2.8.2402.zip](https://files.cnblogs.com/files/kellynic/Redis-x64-2.8.2402.zip)，点击 start.bat 运行；

> cd imServer && dotnet run

> cd web && dotnet run

> 打开多个浏览器，访问 http://127.0.0.1:5000 发送群消息

![image](https://user-images.githubusercontent.com/16286519/62152387-05980c00-b335-11e9-8b6d-3f6d03bb3629.png)

# 设计思路

终端（如浏览器） 使用 webSocket 连接 imServer；

imServer 根据 clientId 分区管理 webSocket 连接，可群集部署；

webApi 或其他应用端，使用 ImHelper 调用相关方法（如：SendMessage、群聊相关方法），将数据推至 Redis Channel；

imServer 订阅 Redis Channel，收到消息后向终端（如浏览器）推送消息；

1、可缓解并发推送消息过多的问题；

2、可解决连接数过多的问题；

3、解决业务和通讯分离，结构更加清淅；

> imServer 充当消息转发，维护连接，代码万年不变不需要重启维护

> webApi 负责所有业务

### webSocket

如果浏览器使用 webSocket ，iOS 使用其他协议，协议不一致的后果很严重（难维护）。

建议所有端都使用 webSocket 协议，adorid/ios/h5/小程序 全部支持 webSocket 客户端。

### 业务通讯

IM 系统一般涉及【我的好友】、【我的群】、【历史消息】等等。。

那么，imServer与业务方(webApi)该保持何种关系呢？

用户A向好友B发送消息，分析一下：

* 需要判断B是否为A好友；
* 需要判断A是否有权限；
* 等等。。

诸如此类业务判断会很复杂，如果使用imServer做业务协议，它是不是会变成巨无霸难以维护？

又如获取历史聊天记录，难道客户端要先webSocket.send('gethistory')，再在onmessage里定位回调处理？

### 发送消息

业务和推送分离的设计，即 imServer 只负责推送工作，webApi 负责业务。

用户A向B发消息：终端A ajax -> webApi -> imServer -> 终端B webSocket.onmessage；

获取历史消息：客户端请求业务方(webApi)接口，返回json(历史消息)。

背后采用 redis 轻量级的订阅发布功能，实现消息缓冲发送，方案必备之一，后期可更换为其他技术。比如 webApi 业务发需要通知1000个人，若不用消息缓冲，会对 webApi 应用程序整体将造成性能损耗。

> 还有使用 redis 存储一些数据，如在线 clientId，频道信息。

### 集群分区

单个 imServer 实例支持多少个客户端连接，两千个没问题？如果在线用户有10万人，怎么办？？？

部署 4 个 imServer：

imServer1 订阅 redisChanne1

imServer2 订阅 redisChanne2

imServer3 订阅 redisChanne3

imServer4 订阅 redisChanne4

业务方(webApi) 根据接收方的 clientId 后四位 16 进制与节点总数取模，定位到对应的 redisChannel，进行 redis->publish 操作将消息定位到相应的 imServer。

每个 imServer 管理着对应的终端连接，当接收到 redis 订阅消息后，向对应的终端连接推送数据。

### 事件消息

IM 系统比较常用的有上线、下线，在 imServer 层才能准确捕捉事件，但业务代码不合适在这上面编写了。

此时采用 redis 发布订阅技术，将上线、下线等事件向指定频道发布，业务方(webApi) 通过 ImHelper.EventBus 方法进行订阅捕捉。

![image](https://user-images.githubusercontent.com/16286519/62150466-a46e3980-b330-11e9-86f3-d050160f0913.png)

### A向B发文件的例子

1、A向 webapi 传文件

2、webapi 告诉 imServer，A向B正在传文件，ImHelper.SendMessage(B, "A正在给传送文件...")

3、B收到消息，A正在传文件

4、webapi 文件接收完成时告诉imServer，A向B文件传输完毕，ImHelper.SendMessage(B, "A文件传输完毕（含文件链接）")

5、B收到消息，A文件传输完毕（含文件链接）

# 有感而发

为什么说 signalr 不合适做 im？

im 的特点必定是长连接，轮训的功能用不上。

因为他是双工通讯的设计，用 hub.invoke 发送命令给服务端处理业务，其他就和 ajax 差不多，用来代替 ajax 减少 http 请求数量比较看好。

但是过多使用 hub，signalr 服务端会被业务入侵严重，业务变化频繁后不得不重新发布版本，每次部署所有终端都会断开连接，遇到5分钟发一次业务补丁的时候，类似离线和上线提示好友的功能就无法实现。

ImCore 的设计是业务和推送分离，即 imServer 永不更新重启，业务全部在 webApi 上编写，终端连接的是 imServer 就不会频繁重启的问题。
