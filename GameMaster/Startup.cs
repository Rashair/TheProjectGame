using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Events;
using Shared.Messages;

using static System.Environment;

namespace GameMaster
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
            string folderName = Path.Combine("TheProjectGameLogs", DateTime.Today.ToString("yyyy-MM-dd"), "GameMaster");
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string fileName = $"gm-{DateTime.Now:HH-mm-ss}-{processId:000000}.log";
            string path = Path.Combine(GetFolderPath(SpecialFolder.MyDocuments), folderName, fileName);
            return new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.File(
               path: path,
               rollOnFileSizeLimit: true,
               outputTemplate: LoggerTemplate)
               .WriteTo.Console(outputTemplate: LoggerTemplate)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
               .CreateLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.TryAddSingleton<ILogger>(GetLogger());

            // TODO: Restore if visualisation will be added
            // services.AddSingleton<TcpSocketManager<BackendMessage>>();
            services.AddSingleton<ISocketManager<TcpClient, GMMessage>, TcpSocketManager<GMMessage>>();
            services.AddSingleton<BufferBlock<PlayerMessage>>();

            GameConfiguration conf;
            string path = Configuration.GetValue<string>("GameConfigPath");
            if (File.Exists(path))
            {
                conf = new GameConfiguration(path);
            }
            else
            {
                conf = new GameConfiguration();
                Configuration.Bind("DefaultGameConfig", conf);
            }
            services.AddSingleton(conf);

            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();
            services.AddHostedService<TcpListenerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
