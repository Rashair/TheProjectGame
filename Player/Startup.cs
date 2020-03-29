using System;
using System.Threading.Tasks.Dataflow;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Player.Clients;
using Player.Models;
using Player.Models.Strategies;
using Player.Services;
using Serilog;
using Serilog.Events;
using Shared.Messages;

using static System.Environment;

namespace Player
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {SourceContext}{NewLine}[{Level}] {Message}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.File(
               path: GetFolderPath(SpecialFolder.MyDocuments)
               + "\\TheProjectGameLogs\\Player_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt",
               rollOnFileSizeLimit: true,
               outputTemplate: template)
                .WriteTo.Console(outputTemplate: template)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
               .CreateLogger();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            PlayerConfiguration conf = new PlayerConfiguration();
            Configuration.Bind("DefaultPlayerConfig", conf);
            services.AddSingleton(conf);

            services.AddSingleton<ISocketClient<GMMessage, PlayerMessage>, WebSocketClient<GMMessage, PlayerMessage>>();
            services.AddSingleton<BufferBlock<GMMessage>>();
            services.AddSingleton<PlayerConfiguration>();
            services.AddSingleton<IStrategy, DummyStrategy>();

            services.AddHostedService<SocketService>();

            services.AddSingleton<Player.Models.Player>();
            services.AddHostedService<PlayerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
