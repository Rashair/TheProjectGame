using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Player.Models;
using Player.Services;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace Player.Tests
{
    public class SocketServiceTests
    {
        private readonly ILogger logger = MockGenerator.Get<ILogger>();

        [Fact(Timeout = 3000)]
        public async Task TestExecuteAsyncShouldReceiveAndSendMessages()
        {
            // Arrange
            int numberOfMessages = 5;

            var clientMock = new Mock<ISocketClient<Message, Message>>();
            BufferBlock<Message> queue = new BufferBlock<Message>();
            ServiceSynchronization context = new ServiceSynchronization(0, 1);

            var calls = 0;
            clientMock.Setup(m => m.ReceiveAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (calls < numberOfMessages)
                    return Task.FromResult((true, new Message()));
                else
                    return Task.FromResult((false, new Message()));
            })
            .Callback(() =>
            {
                calls++;
            });

            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(logger);
            services.AddSingleton(clientMock.Object);
            services.AddSingleton<PlayerConfiguration>();
            services.AddSingleton(queue);
            services.AddSingleton(Mock.Of<IApplicationLifetime>());
            services.AddSingleton(context);

            services.AddHostedService<SocketService>();
            var serviceProvider = services.BuildServiceProvider();
            var hostedService = (SocketService)serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 1500;
            Task socketTask = Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });
            Task syncTask = Task.Run(async () =>
            {
                await context.SemaphoreSlim.WaitAsync(CancellationToken.None);
                context.SemaphoreSlim.Release();
            });

            await Task.WhenAll(new[] { socketTask, syncTask });

            // Assert
            Assert.Equal(numberOfMessages + 1, queue.Count);
        }
    }
}
