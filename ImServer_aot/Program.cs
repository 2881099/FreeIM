using Newtonsoft.Json;
using System.Text;

Console.WriteLine(typeof((Guid clientId, string clientMetaData)));
Console.WriteLine(typeof(Tuple<Guid, string>));
var json = JsonConvert.SerializeObject((Guid.NewGuid(), new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }, "xxx", true));
Console.Write(json);
Console.WriteLine(JsonConvert.DeserializeObject<(Guid, List<Guid>, string, bool)>(json));

json = JsonConvert.SerializeObject((Guid.NewGuid(), "xxx"));
Console.Write(json);
Console.WriteLine(JsonConvert.DeserializeObject<(Guid, string)>(json));

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();
var configuration = app.Configuration;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding("GB2312");
Console.InputEncoding = Encoding.GetEncoding("GB2312");

app.UseDeveloperExceptionPage();

var imOptions = new ImServerOptions
{
	Redis = new FreeRedis.RedisClient(configuration["ImServerOption:RedisClient"]),
	Servers = configuration["ImServerOption:Servers"].Split(";"),
	Server = configuration["ImServerOption:Server"]
};
app.UseFreeImServer(imOptions);

var applicationLifeTime = app.Services.GetService<IHostApplicationLifetime>();
applicationLifeTime.ApplicationStopping.Register(() =>
{
	imOptions.Redis.Dispose();
});
app.Run($"http://{imOptions.Server}");
