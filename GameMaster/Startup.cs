using System;
using System.IO;
using System.Threading.Tasks.Dataflow;

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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Clients;
using Shared.Messages;
using Shared.Models;

namespace GameMaster;

public class Startup
{
    public const string LoggerTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {SourceContext}{NewLine}[{Level}] {Message}{NewLine}{Exception}";

    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private ILogger GetLogger(bool verbose)
    {
        LoggerLevel level = new LoggerLevel();
        Configuration.Bind("Serilog:MinimumLevel", level);

        string folderName = Path.Combine("TheProjectGameLogs", DateTime.Today.ToString("yyyy-MM-dd"), "GameMaster");
        int processId = Environment.ProcessId;
        string fileName = $"gm-{DateTime.Now:HH-mm-ss}-{processId:000000}.log";
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folderName, fileName);
        var logConfig = new LoggerConfiguration()
           .Enrich.FromLogContext()
           .WriteTo.File(
           path: path,
           rollOnFileSizeLimit: true,
           outputTemplate: LoggerTemplate)
           .WriteTo.Console(outputTemplate: LoggerTemplate)
            .MinimumLevel.Override("Microsoft", level.Override.Microsoft)
            .MinimumLevel.Override("System", level.Override.System);
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
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();

        // TODO: Restore if visualisation will be added
        // services.AddSingleton<TcpSocketManager<BackendMessage>>();
        services.AddSingleton<ISocketClient<Message, Message>, TcpSocketClient<Message, Message>>();
        services.AddSingleton<BufferBlock<Message>>();
        services.AddSingleton<WebSocketManager<ClientMessage>>();

        var conf = GameConfiguration.GetConfiguration(Configuration);
        services.AddSingleton(conf);

        services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = "ClientApp/build";
        });

        services.TryAddSingleton<ILogger>(GetLogger(conf.Verbose ??
            Configuration.GetValue<bool>("DefaultGameConfig:Verbose")));

        services.AddSingleton<GM>();
        services.AddHostedService<SocketService>();
        services.AddHostedService<GMService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

        app.UseRouting();

        app.UseStaticFiles();
        app.UseSpaStaticFiles();

        app.UseHttpsRedirection();
        app.UseWebSockets();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(name: "default",
                pattern: "{controller}/{action=Index}/{id?}");
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
