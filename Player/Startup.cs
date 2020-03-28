﻿using System.Threading.Tasks.Dataflow;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Player.Clients;
using Player.Models;
using Player.Services;
using Shared.Messages;

namespace Player
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISocketClient<GMMessage, PlayerMessage>, WebSocketClient<GMMessage, PlayerMessage>>();
            services.AddSingleton<BufferBlock<GMMessage>>();
            services.AddSingleton<Configuration>();

            services.AddHostedService<SocketService>();
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
