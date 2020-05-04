using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Castle.Core.Logging;
using CommunicationServer.Models;
using CommunicationServer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Managers;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace CommunicationServerTests
{
    public class CommunicatorTests
    {
        [Fact]
        public async Task TestExecuteAsyncShouldReceivePlayerMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            AddLogging(services);
            var conf = new ServerConfigurations() { GMPort = 4000, ListenerIP = "127.0.0.1", PlayerPort = 44370 };
            services.AddSingleton(conf);
            services.AddSingleton<ISocketClient<PlayerMessage, GMMessage>, TcpSocketClient<PlayerMessage, GMMessage>>();
            services.AddSingleton<ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>, TcpSocketManager<PlayerMessage, GMMessage>>();

            BufferBlock<Message> queue = new BufferBlock<Message>();
            int numberOfMessages = 10;
            for (int i = 0; i < numberOfMessages; i++)
                queue.Post(new PlayerMessage());

            var clientMock = new Mock<ISocketClient<GMMessage, PlayerMessage>>();
            int calls = 0;
            clientMock.Setup(m => m.SendAsync(new PlayerMessage(), It.IsAny<CancellationToken>()))
           .Returns(() =>
           {
               return Task.FromResult(true);
           });

            ServiceShareContainer shareContainer = new ServiceShareContainer();
            shareContainer.GMClient = clientMock.Object;
            services.AddSingleton<ServiceShareContainer>(shareContainer);
            services.AddSingleton(Mock.Of<Microsoft.Extensions.Hosting.IApplicationLifetime>());
            services.AddSingleton<BufferBlock<Message>>();
            services.AddSingleton(new ServiceSynchronization(1, 1));
            services.AddSingleton<BufferBlock<Message>>(queue);

            services.AddHostedService<CommunicationService>();
            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task TestExecuteAsyncShouldReceiveGMMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            AddLogging(services);
            var conf = new ServerConfigurations() { GMPort = 4000, ListenerIP = "127.0.0.1", PlayerPort = 44370 };
            services.AddSingleton(conf);
            services.AddSingleton<ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>, TcpSocketManager<PlayerMessage, GMMessage>>();

            BufferBlock<Message> queue = new BufferBlock<Message>();
            int numberOfMessages = 9;
            for (int i = 0; i < numberOfMessages; i++)
                queue.Post(new GMMessage());

            var clientMock = new Mock<ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>>();
            int calls = 0;
            clientMock.Setup(m => m.SendMessageAsync(0, new GMMessage(), It.IsAny<CancellationToken>()))
           .Returns(() =>
           {
               return Task.FromResult(true);
           });

            services.AddSingleton<ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>>(clientMock.Object);
            services.AddSingleton<ServiceShareContainer>();
            services.AddSingleton(Mock.Of<Microsoft.Extensions.Hosting.IApplicationLifetime>());
            services.AddSingleton<BufferBlock<Message>>();
            services.AddSingleton(new ServiceSynchronization(1, 1));
            services.AddSingleton<BufferBlock<Message>>(queue);

            services.AddHostedService<CommunicationService>();
            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddSingleton(MockGenerator.Get<Serilog.ILogger>());
        }
    }
}
