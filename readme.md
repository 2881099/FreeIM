# 高性能简易即时聊天系统

本项目利用 redis 订阅与发布特性，巧妙的实现高性能im系统。

下载源码后的运行方法：

> 运行环境：.NETCore 2.1 + redis-server 2.8

> [下载Redis-x64-2.8.2402.zip](https://files.cnblogs.com/files/kellynic/Redis-x64-2.8.2402.zip)，点击 start.bat 运行；或者修改 imServer、web 下面 appsettings.json redis 配置，指向可用的redis-server

> cd imServer && dotnet run --urls="http://0.0.0.0:6001"

> cd web && dotnet run --urls="http://0.0.0.0:5555"

> 打开多个浏览器，访问 http://127.0.0.1:5555 发送群消息

# 设计思路

imServer 是 websocket 服务中心，可部署多实例，按用户id分区连接；

web或其他程序，如果向某用户推送消息，只需向 redis-channel 发送消息即可；

imServer 订阅了相应的频道，收到消息，指派 websocket 向客户端发送消息；

1、可缓解并发推送消息过多的问题；

2、可解决连接数过多的问题；

客户端连接流程：client -> websocket -> imserver

imserver 订阅消息：client <- imserver <- redis channel

推送消息流程：web1 -> sendmsg方法 -> redis channel -> imserver

imserver 充当消息转发，及维护连接中心，代码万年不变不需要重启维护；

### socket选型

最二的办法是浏览器端使用websocket，其他端socket，这么混乱的设计最终将非常难维护。

所以强烈建议所有端都使用websocket协议，adorid/ios/h5/小程序全部支持websocket客户端。

### 业务与通讯协议

im系统一般涉及【我的好友】、【我的群】、【历史消息】等等。。

那么，imServer与业务方(web)该保持何种关系呢？

用户A向好友B发送消息，分析一下：

* 需要判断B是否为A好友；
* 需要判断A是否有权限；
* 等等。。

诸如此类业务判断会很复杂，我们试想一下，如果使用imServer做业务协议，它是不是会变成巨无霸难以维护。

又假如获取历史记录，难道客户端要先websocket.send('gethistory')，再在onmessage里定位回调处理？

这样做十分之二。。。imServer全是业务

---

咱这样设计，所有用户的主动行为走业务方(web)，imServer只负责即时消息推送。什么意思？

用户A向好友B发送消息：客户端请求业务方(web)接口，由业务方(web)后端向imServer发起推送请求，imServer收到指令后，向前端用户B的websocket发送数据，用户B收到了消息。

获取历史消息：客户端请求业务方(web)接口，返回json(历史消息)

回执：用户A如何知道消息发送状态（成功或失败或不在线）？imServer端向用户B发送消息时，把状态以消息的方式推给用户A即可（按上面的逻辑），具体请看源码吧。。。

### web通知imServer性能优化

采用消息队列，redis的发布订阅最为轻量。

### 实现多节点部署

单个imServer实例支持多少websocket连接，几百个没问题吧，好。。。

如果系统在线用户有1万人，怎么办？？？

可以根据id的hash分区，比如部署4个imServer：

* imServer1 订阅 redisChanne1
* imServer2 订阅 redisChanne2
* imServer3 订阅 redisChanne3
* imServer4 订阅 redisChanne4

业务方(web)端根据接收方的id的hash分区算法，定位到对应的redisChannel，这样publish就可以将消息定位到相应的imServer了
