using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;

namespace web
{

    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");

            app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(config => config.MapControllers());
            app.UseDefaultFiles();
            app.UseStaticFiles();

            ImHelper.Initialization(new ImClientOptions
            {
                Redis = new FreeRedis.RedisClient("127.0.0.1:6379,poolsize=10"),
                Servers = new[] { "127.0.0.1:6001" }
            });

            ImHelper.Instance.OnSend += (s, e) => 
                Console.WriteLine($"ImClient.SendMessage(server={e.Server},data={JsonConvert.SerializeObject(e.Message)})");

            ImHelper.EventBus(
                t =>
                {
                    Console.WriteLine(t.clientId + "上线了");
                    var onlineUids = ImHelper.GetClientListByOnline();
                    ImHelper.SendMessage(t.clientId, onlineUids, $"用户{t.clientId}上线了");
                }, 
                t => Console.WriteLine(t.clientId + "下线了"));
        }
    }
}
