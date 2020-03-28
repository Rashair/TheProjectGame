using System;
using System.Threading.Tasks.Dataflow;

using static System.Environment;

using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Models.Messages;
using GameMaster.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Shared.Messages;

namespace GameMaster
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
               + "\\TheProjectGameLogs\\Main_" + DateTime.Today.ToString("dd_MM_yyyy") + ".txt",
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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddSingleton<WebSocketManager<BackendMessage>>();
            services.AddSingleton<WebSocketManager<GMMessage>>();
            services.AddSingleton<BufferBlock<PlayerMessage>>();

            services.AddSingleton<Configuration>();
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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
            app.UseWebSockets();

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
