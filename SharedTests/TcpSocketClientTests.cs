using Moq;
using Serilog;
using Shared.Clients;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace Shared.Tests
{
    public class TcpSocketClientTests
    {
        private readonly ILogger logger;
        private readonly Mock<ITcpClient> tcpClientMock;

        public TcpSocketClientTests()
        {
            logger = MockGenerator.Get<ILogger>();
            tcpClientMock = new Mock<ITcpClient>() { DefaultValue = DefaultValue.Mock };
        }

        [Fact]
        public void IsOpen_Test()
        {
            // Arrange
            tcpClientMock.Setup(s => s.Connected).Returns(false);
            var socketClient = new TcpSocketClient<PlayerMessage, GMMessage>(tcpClientMock.Object, logger);

            // Act
            bool open = socketClient.IsOpen;

            // Assert
            Assert.False(open, "Client should not be open when socket is closed");
        }
    }
}
