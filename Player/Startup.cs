using System;
using System.IO;
using System.Threading.Tasks.Dataflow;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Player.Models;
using Player.Services;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;

namespace Player
{
    public class Startup
    {
        public const string LoggerTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {SourceContext}{NewLine}[{Level}] {Message}{NewLine}{Exception}";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private ILogger GetLogger(Team team, bool verbose)
        {
            LoggerLevel level = new LoggerLevel();
            Configuration.Bind("Serilog:MinimumLevel", level);

            string folderName = Path.Combine("TheProjectGameLogs", DateTime.Today.ToString("yyyy-MM-dd"), "Player");
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string teamId = team.ToString().Substring(0, 3);
            string fileName = $"{teamId}-{DateTime.Now:HH-mm-ss}-{processId:000000}.log";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                folderName, fileName);
            var logConfig = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.File(
               path: path,
               rollOnFileSizeLimit: true,
               outputTemplate: LoggerTemplate)
               .WriteTo.Console(outputTemplate: LoggerTemplate)
                .MinimumLevel.Override("Microsoft", level.Override.Microsoft)
                .MinimumLevel.Override("System", level.Override.System);
            level.SetMinimumLevel(logConfig);

            if (verbose)
            {
                logConfig.MinimumLevel.Verbose();
            }
            else
            {
                level.SetMinimumLevel(logConfig);
            }
            return logConfig.CreateLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkId=398940
        public void ConfigureServices(IServiceCollection services)
        {
            PlayerConfiguration conf = new PlayerConfiguration();
            Configuration.Bind("DefaultPlayerConfig", conf);

            // For console override.
            Configuration.Bind(conf);
            services.AddSingleton(conf);

            // 'Try' for tests override
            var logger = GetLogger(conf.TeamID, conf.Verbose);
            services.TryAddSingleton<ILogger>(logger);

            services.AddSingleton<ISocketClient<Message, Message>, TcpSocketClient<Message, Message>>();
            services.AddSingleton<BufferBlock<Message>>();
            services.AddSingleton<Models.Player>();

            var sync = new ServiceSynchronization(0, 1);
            services.AddSingleton(sync);
            services.AddHostedService<SocketService>();
            services.AddHostedService<PlayerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
