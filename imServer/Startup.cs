using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace imServer
{

    public class Startup
    {

        public Startup()
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseImServer(new ImServerOptions
            {
                Redis = new CSRedis.CSRedisClient("127.0.0.1:6379,poolsize=5"),
                Servers = new[] { "127.0.0.1:6001" },
                Server = "127.0.0.1:6001"
            });
        }
    }
}
