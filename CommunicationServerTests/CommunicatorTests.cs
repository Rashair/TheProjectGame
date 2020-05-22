using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using CommunicationServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared;
using Shared.Clients;
using Shared.Converters;
using Shared.Enums;
using Shared.Managers;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace CommunicationServerTests
{
    public class CommunicatorTests
    {
        private Mock<ISocketManager<ISocketClient<Message, Message>, Message>> GetManagerMock(
            Action<Message> action = null)
        {
            var managerMock = new Mock<ISocketManager<ISocketClient<Message, Message>, Message>>();
            managerMock.Setup(m => m.SendMessageAsync(It.IsAny<int>(), It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns<int, Message, CancellationToken>((id, message, token) =>
                {
                    action?.Invoke(message);
                    return Task.FromResult(true);
                });

            return managerMock;
        }

        private Mock<ISocketClient<Message, Message>> GetGMClientMock(Action<Message> action = null)
        {
            var clientMock = new Mock<ISocketClient<Message, Message>>();
            clientMock.Setup(m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                .Returns<Message, CancellationToken>((message, token) =>
                {
                    action?.Invoke(message);
                    return Task.FromResult(true);
                });

            return clientMock;
        }

        private BufferBlock<Message> PrepareQueue(MessageID messageID, int msgCount = 10)
        {
            BufferBlock<Message> queue = new BufferBlock<Message>();
            for (int i = 0; i < msgCount; ++i)
            {
                queue.Post(new Message()
                {
                    MessageID = messageID,
                    AgentID = 1,
                    Payload = messageID.GetPayload("")
                });
            }

            return queue;
        }

        private IServiceCollection GetServices(ISocketManager<ISocketClient<Message, Message>, Message> manager,
            ISocketClient<Message, Message> gmClient)
        {
            IServiceCollection services = new ServiceCollection();
            var conf = new ServerConfiguration() { GMPort = 44360, ListenerIP = "127.0.0.1", PlayerPort = 44370 };
            services.AddSingleton(conf);
            services.AddSingleton(MockGenerator.Get<Serilog.ILogger>());

            services.AddSingleton(manager);

            ServiceShareContainer shareContainer = new ServiceShareContainer
            {
                GMClient = gmClient
            };
            services.AddSingleton(shareContainer);

            services.AddSingleton(Mock.Of<IApplicationLifetime>());
            services.AddSingleton(new ServiceSynchronization(1, 1));

            services.AddHostedService<CommunicationService>();

            return services;
        }

        [Fact]
        public async Task TestExecuteAsyncShouldSendPlayerMessages()
        {
            // Arrange
            Message msg = null;
            void SetMessage(Message posted)
            {
                msg = posted;
            }
            var managerMock = GetManagerMock();
            var clientMock = GetGMClientMock(SetMessage);

            IServiceCollection services = GetServices(managerMock.Object, clientMock.Object);

            int msgCount = 9;
            var msgID = MessageID.JoinTheGame;
            var queue = PrepareQueue(msgID, msgCount);
            services.AddSingleton(queue);

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 1000;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);

            Assert.NotNull(msg);
            Assert.Equal(msgID, msg.MessageID);

            managerMock.Verify(m => m.SendMessageAsync(It.IsAny<int>(), It.IsAny<Message>(),
                It.IsAny<CancellationToken>()), Times.Never(),
                $"Should not send messages to players");
            clientMock.Verify(m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
                Times.Exactly(msgCount),
                $"Should send exactly {msgCount} messages");
        }

        [Fact]
        public async Task TestExecuteAsyncShouldSendGMMessages()
        {
            // Arrange
            Message msg = null;
            void SetMessage(Message posted)
            {
                msg = posted;
            }
            var managerMock = GetManagerMock(SetMessage);
            var clientMock = GetGMClientMock();

            IServiceCollection services = GetServices(managerMock.Object, clientMock.Object);

            int msgCount = 10;
            var msgID = MessageID.JoinTheGameAnswer;
            var queue = PrepareQueue(msgID, msgCount);
            services.AddSingleton(queue);

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 1000;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);

            Assert.NotNull(msg);
            Assert.Equal(msgID, msg.MessageID);

            managerMock.Verify(m => m.SendMessageAsync(It.IsAny<int>(), It.IsAny<Message>(),
              It.IsAny<CancellationToken>()), Times.Exactly(msgCount),
              $"Should send exactly {msgCount} messages to players");
            clientMock.Verify(m => m.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
                Times.Never(),
                "Should not send messages to GM");
        }
    }
}
