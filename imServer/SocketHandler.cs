using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace imServer {
	public class WebSocketHandler {
		public const int BufferSize = 4096;

		public WebSocket socket;
		public string uid;

		WebSocketHandler(WebSocket socket, string uid) {
			this.socket = socket;
			this.uid = uid;
		}

		static object _websockets_lock = new object();
		static Dictionary<string, List<WebSocketHandler>> _websockets = new Dictionary<string, List<WebSocketHandler>>();

		static async Task Acceptor(HttpContext hc, Func<Task> n) {
			if (!hc.WebSockets.IsWebSocketRequest) return;

			string token = hc.Request.Query["token"];
			if (string.IsNullOrEmpty(token)) return;
			var token_value = await RedisHelper.GetAsync($"webchat_token_{token}");
			if (string.IsNullOrEmpty(token_value)) return; //用户需先访问 /webchat/selectserver 接口确定服务器，并且返回 token
			var data = Newtonsoft.Json.JsonConvert.DeserializeObject<(string uid, string ip)>(token_value);
			Guid uid = Guid.Parse(data.uid);

			var socket = await hc.WebSockets.AcceptWebSocketAsync();
			var sh = new WebSocketHandler(socket, data.uid);

			List<WebSocketHandler> list = null;
			lock (_websockets_lock) {
				if (_websockets.TryGetValue(data.uid, out list) == false)
					_websockets.Add(data.uid, list = new List<WebSocketHandler>());
				list.Add(sh);
			}
			await RedisHelper.HIncrByAsync("online", data.uid, 1);

			var buffer = new byte[BufferSize];
			var seg = new ArraySegment<byte>(buffer);
			try {
				while (socket.State == WebSocketState.Open && _websockets.ContainsKey(data.uid)) {
					var incoming = await socket.ReceiveAsync(seg, CancellationToken.None);
					var outgoing = new ArraySegment<byte>(buffer, 0, incoming.Count);
					//foreach (SocketHandler sh in _websockets.Values) {
					//	await sh.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
					//}
				}
				socket.Abort();
			} catch {

			}
			lock (_websockets_lock) {
				list.Remove(sh);
				if (list.Count == 0) _websockets.Remove(data.uid);
			}
			await RedisHelper.EvalAsync($"if redis.call('HINCRBY', KEYS[1], '{data.uid}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{data.uid}') end return 1", "online");
		}

		public static void RedisSubsrcMessage(CSRedis.CSRedisClient.SubscribeMessageEventArgs e) {
			//注意：redis服务重启以后，需要重新启动服务，不然redis通道无法重连
			try {
				var msg = WebChatMessage.Parse(e.Body);
				switch (msg.Type) {
					case WebChatMessageType.发送消息:
						var data = msg.GetData<(string sender, string[] receives, string content, bool receipt)>();
						Console.WriteLine($"收到消息：{data.content}" + (data.receipt ? "【需回执】" : ""));

						var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data.content));
						foreach (var uid in data.receives) {
							List<WebSocketHandler> sock = null;
							if (_websockets.TryGetValue(uid, out sock) == false) {
								//Console.WriteLine($"websocket{uid} 离线了，{data.content}" + (data.receipt ? "【需回执】" : ""));
								if (uid != data.sender && data.receipt) {
									WebChatHelper.SendMsg(Guid.Parse(uid), new Guid[] { Guid.Parse(data.sender) }, new {
										data.content,
										receipt = "用户不在线"
									});
								}
								//未找到socket
								continue;
							}

							WebSocketHandler[] sockarray;
							try {
								sockarray = sock.ToArray();
							} catch {
								lock (_websockets_lock)
									sockarray = sock.ToArray();
							}

							//如果接收消息人是发送者，并且接口端只有1个以下，则不发送
							//只有接口者为多端时，才转发消息通知其他端
							if (uid == data.sender && sockarray.Length <= 1) continue;

							foreach (WebSocketHandler sh in sockarray)
								sh.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

							if (uid != data.sender && data.receipt) {
								WebChatHelper.SendMsg(Guid.Parse(uid), new Guid[] { Guid.Parse(data.sender) }, new {
									data.content,
									receipt = "发送成功"
								});
							}
						}
						break;
					case WebChatMessageType.下线:
						lock (_websockets_lock)
							_websockets.Remove(string.Concat(msg.Data));
						break;
					case WebChatMessageType.上线:
						break;
				}
			} catch (Exception ex) {
				Console.WriteLine($"订阅方法出错了：{ex.Message}");
			}
		}

		static IConfiguration Configuration;

		public static void Map(IApplicationBuilder app) {
			Configuration = app.ApplicationServices.GetService(typeof(IConfiguration)) as IConfiguration;
			RedisHelper.Subscribe(($"webchat_{Configuration["redischannel"]}", RedisSubsrcMessage));

			app.UseWebSockets();
			app.Use(WebSocketHandler.Acceptor);
		}
	}
}
