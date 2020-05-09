﻿using System;
using System.IO;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using CommunicationServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shared;
using Shared.Clients;
using Shared.Managers;
using Shared.Messages;
using Shared.Models;

using static System.Environment;

namespace CommunicationServer
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

        private ILogger GetLogger()
        {
            LoggerLevel level = new LoggerLevel();
            Configuration.Bind("Serilog:MinimumLevel", level);
            
            string folderName = Path.Combine("TheProjectGameLogs", DateTime.Today.ToString("yyyy-MM-dd"), "CommunicationServer");
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string fileName = $"cs-{DateTime.Now:HH-mm-ss}-{processId:000000}.log";
            string path = Path.Combine(GetFolderPath(SpecialFolder.MyDocuments), folderName, fileName);
            var logConfig = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.File(
               path: path,
               rollOnFileSizeLimit: true,
               outputTemplate: LoggerTemplate)
               .WriteTo.Console(outputTemplate: LoggerTemplate)
                .MinimumLevel.Override("Microsoft", level.Microsoft)
                .MinimumLevel.Override("System", level.System);
            level.SetMinimumLevel(logConfig);

            return logConfig.CreateLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkId=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var logger = GetLogger();
            services.TryAddSingleton<ILogger>(logger);
            var p = logger;

            ServerConfigurations conf = new ServerConfigurations();
            Configuration.Bind("DefaultCommunicationServerConfig", conf);
            Configuration.Bind(conf);  // For console override;
            services.AddSingleton(conf);

            services.AddSingleton<ServiceShareContainer>();
            services.AddSingleton<ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>,
                TcpSocketManager<PlayerMessage, GMMessage>>();
            services.AddSingleton<BufferBlock<Message>>();

            var sync = new ServiceSynchronization(0, 2);
            services.AddSingleton(sync);
            services.AddHostedService<GMTcpSocketService>();
            services.AddHostedService<PlayersTcpSocketService>();
            services.AddHostedService<CommunicationService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
