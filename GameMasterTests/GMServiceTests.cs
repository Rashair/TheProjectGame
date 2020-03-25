using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models;
using GameMaster.Services;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Messages;
using Xunit;

using static GameMaster.Tests.Helpers.ReflectionHelpers;

namespace GameMaster.Tests
{
    public class GMServiceTests
    {
        [Fact(Timeout = 3000)]
        public async Task TestExecuteAsyncShouldWaitForStartAndReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
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
            var startGame = GetMethod("StartGame");

            // Act
            int delay = 1000;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);

                startGame.Invoke(gameMaster, null);

                await Task.Delay(hostedService.WaitForStartDelay + 500);
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
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
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
            var hostedService = serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);

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
            services.AddSingleton<ILogger<GM>, MockLogger<GM>>();
        }
    }
}
