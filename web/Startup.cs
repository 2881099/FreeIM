using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            services.AddMvc();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");
            loggerFactory.AddConsole(LogLevel.Error);

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
            app.UseSwagger().UseSwaggerUI();

            ImHelper.Initialization(new ImClientOptions
            {
                Redis = new CSRedis.CSRedisClient("127.0.0.1:6379,poolsize=5"),
                Servers = new[] { "127.0.0.1:6001" }
            });

            ImHelper.EventBus(t => Console.WriteLine(t.clientId + "上线了"), t => Console.WriteLine(t.clientId + "下线了"));
        }
    }
}
