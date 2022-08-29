using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;

namespace imServer
{

    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");
            
            app.UseDeveloperExceptionPage();

            app.UseFreeImServer(new ImServerOptions
            {
                Redis = new FreeRedis.RedisClient(Configuration["ImServerOption:RedisClient"]),
                Servers = Configuration["ImServerOption:Servers"].Split(";"),
                Server = Configuration["ImServerOption:Server"]
            });
        }
    }
}
