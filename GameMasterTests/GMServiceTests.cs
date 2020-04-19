using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models;
using GameMaster.Services;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using Shared.Clients;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class GMServiceTests
    {
        [Fact(Timeout = 4000)]
        public async Task TestExecuteAsyncShouldWaitForStartAndReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockGameConfiguration();
            services.AddSingleton<GameConfiguration>(conf);
            services.AddSingleton(Mock.Of<IApplicationLifetime>());
            services.AddSingleton<ISocketClient<PlayerMessage, GMMessage>, TcpSocketClient<PlayerMessage, GMMessage>>();
            int messagesNum = 10;
            var queue = new BufferBlock<PlayerMessage>();
            for (int i = 0; i < messagesNum; ++i)
            {
                queue.Post(new PlayerMessage());
            }
            services.AddSingleton(queue);
            AddLogging(services);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = (GMService)serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();

            // Act
            int delay = 1000;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);

                gameMaster.Invoke("InitGame");

                await Task.Delay(hostedService.WaitForInitDelay + 500);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
        }

        [Fact(Timeout = 1500)]
        public async Task TestExecuteAsyncShouldReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            AddLogging(services);
            var conf = new MockGameConfiguration();
            services.AddSingleton(Mock.Of<IApplicationLifetime>());
            services.AddSingleton<GameConfiguration>(conf);
            services.AddSingleton<ISocketClient<PlayerMessage, GMMessage>, TcpSocketClient<PlayerMessage, GMMessage>>();
            int messagesNum = 10;
            var queue = new BufferBlock<PlayerMessage>();
            for (int i = 0; i < messagesNum; ++i)
            {
                queue.Post(new PlayerMessage());
            }
            services.AddSingleton(queue);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            gameMaster.Invoke("InitGame");

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
            services.AddSingleton(MockGenerator.Get<ILogger>());
        }
    }
}
