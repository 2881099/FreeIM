using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Globalization;

public static class WebChatHelper {

	public static IConfiguration Configuration;

	/// <summary>
	/// 获取用户所在websocket服务端，按uid分区
	/// </summary>
	/// <param name="uid"></param>
	/// <returns></returns>
	public static string GetServer(Guid uid) {
		//负载分区规则：
		//取用户id，前两位字符，转成10进制数字0-255
		//0-63为服务区1
		//64-127为服务区2
		//128-191为服务区3
		//192-255为服务区4
		var servers = Configuration.GetSection("webchat_servers").AsEnumerable().Select(a => a.Value).ToArray();
		var left2 = 1.0 * int.Parse(uid.ToString().Substring(0, 2), NumberStyles.HexNumber) / 64;
		int servers_idx = (int)Math.Floor(left2) + 1;
		if (servers_idx >= servers.Length) servers_idx = 1;
		return servers[servers_idx];
	}

	public static void SendMsg(Guid senderId, Guid[] receiveIds, object message, bool receipt = false) {
		receiveIds = receiveIds.Distinct().ToArray();
		Dictionary<string, List<Guid>> redata = new Dictionary<string, List<Guid>>();

		foreach (var uid in receiveIds) {
			string server = WebChatHelper.GetServer(uid);
			if (redata.ContainsKey(server) == false) redata.Add(server, new List<Guid>());
			redata[server].Add(uid);
		}
		foreach (string channel in redata.Keys) {
			RedisHelper.Publish($"webchat_{channel}", new WebChatMessage {
				Type = WebChatMessageType.发送消息,
				Data = (senderId, redata[channel], JsonConvert.SerializeObject(message), receipt)
			}.ToJson());
		}
	}

	/// <summary>
	/// 操作 user_msg 与 recent_user 表和发送 websocket 消息
	/// </summary>
	/// <param name="content"></param>
	/// <param name="receiveUser"></param>
	//public static object 发送消息_单聊(UserInfo sender, Guid? receiveUser, JToken content, bool receipt = false) {
	//	if (receiveUser == null) return null;
	//	if (sender.Id == receiveUser) return null; //不允许自己给自己发消息
	//	//if (Quanzi_black.GetItem(receiveUser.Value, sender.Id.Value) != null) return null; //被对方拉黑屏蔽无法发送
	//	var msg = new {
	//		Id = Guid.NewGuid(),
	//		Content = content
	//	};

	//	WebChatHelper.发送消息_websocket(sender.Id.Value, new Guid[] { receiveUser.Value, sender.Id.Value }, new {
	//		msg.Id,
	//		msg.Content,
	//		post_user = sender.ToBson()
	//	}, receipt);
	//	return msg;
	//}


	public static string[] GetChannels() {
		//演示不考虑性能
		var chans = RedisHelper.Keys("*WebChatSubscribe*");
		return chans.Select(a => a.Substring(a.IndexOf("WebChatSubscribe") + 16)).ToArray();
	}
	public static void Subscribe(Guid websocketId, string channel) {
		RedisHelper.HSet($"WebChatSubscribe{channel}", websocketId.ToString(), 0);
	}

	public static void Publish(string channel, object message) {
		var websocketIds = RedisHelper.HKeys($"WebChatSubscribe{channel}");
		var offline = new List<string>();
		var span = websocketIds.AsSpan();
		var start = span.Length;
		while(start > 0) {
			start = start - 10;
			var length = 10;
			if (start < 0) {
				length = start + 10;
				start = 0;
			}
			var slice = span.Slice(start, length);
			var hvals = RedisHelper.HMGet("online", slice.ToArray().Select(b => b.ToString()).ToArray());
			for (var a = length - 1; a>=0; a--) {
				if (string.IsNullOrEmpty(hvals[a])) {
					offline.Add(span[start + a]);
					span[start + a] = null;
				}
			}
		}
		//删除离线订阅
		if (offline.Any()) RedisHelper.HDel($"WebChatSubscribe{channel}", offline.ToArray());
		SendMsg(Guid.Empty, websocketIds.Where(a => !string.IsNullOrEmpty(a)).Select(a => Guid.TryParse(a, out var tryuuid) ? tryuuid : Guid.Empty).ToArray(), message);
	}
}

public class WebChatMessage {
	public WebChatMessageType Type { get; set; }
	public object Data { get; set; }

	public T GetData<T>() {
		return JsonConvert.DeserializeObject<T>(string.Concat(this.Data));
	}
	public string ToJson() {
		return JsonConvert.SerializeObject(this);
	}
	public static WebChatMessage Parse(string json) {
		return JsonConvert.DeserializeObject<WebChatMessage>(json);
	}
}

public enum WebChatMessageType {
	下线, 上线, 发送消息
}
 