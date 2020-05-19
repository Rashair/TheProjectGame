using System.IO;
using System.Text;
using System.Threading;

using Moq;
using Newtonsoft.Json;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace Shared.Tests
{
    public class TcpSocketClientTests
    {
        private readonly ILogger logger;
        private readonly Mock<IClient> tcpClientMock;
        private readonly IClient tcpClient;

        public TcpSocketClientTests()
        {
            logger = MockGenerator.Get<ILogger>();
            tcpClientMock = new Mock<IClient>() { DefaultValue = DefaultValue.Mock };
            tcpClient = tcpClientMock.Object;
        }

        [Fact]
        public void IsOpen_OnClientNotOpen_ShouldReturnFalse_Test()
        {
            // Arrange
            tcpClientMock.Setup(s => s.Connected).Returns(false);
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);

            // Act
            bool open = socketClient.IsOpen;

            // Assert
            Assert.False(open, "Client should not be open when socket is not connected");
        }

        [Fact]
        public void GetSocket_Test()
        {
            // Arrange
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);

            // Act
            var socket = socketClient.GetSocket();

            // Assert
            Assert.True(socket == tcpClient,
                "Returned socket should be the same object which was passed via constructor");
        }

        [Fact]
        public async void CloseAsync_Test()
        {
            // Arrange
            tcpClientMock.Setup(s => s.Connected).Returns(true);
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);
            var token = CancellationToken.None;

            // Act
            await socketClient.CloseAsync(token);

            // Assert
            tcpClientMock.Verify(c => c.Close(), Times.Once(),
                "Close from tcpClient should be invoked");
            Assert.False(socketClient.IsOpen, "Client should not be open after close");
        }

        [Fact]
        public async void ConnectAsync_Test()
        {
            // Arrange
            tcpClientMock.Setup(s => s.Connected).Returns(true);
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);
            string host = "";
            int port = 0;
            var token = CancellationToken.None;

            // Act
            await socketClient.ConnectAsync(host, port, token);

            // Assert
            tcpClientMock.Verify(c => c.ConnectAsync(host, port), Times.Once(),
                "Connect from tcpClient should be invoked");
            Assert.True(socketClient.IsOpen, "Client should be open after successful connect");
        }

        [Fact]
        public async void SendAsync_Test()
        {
            // Arrange
            var stream = new MemoryStream(100);
            tcpClientMock.Setup(s => s.Connected).Returns(true);
            tcpClientMock.Setup(s => s.GetStream).Returns(stream);
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);
            string host = "";
            int port = 0;
            var token = CancellationToken.None;
            await socketClient.ConnectAsync(host, port, token);

            var message = new Message(MessageID.Unknown, 1, new EmptyAnswerPayload());

            // Act
            await socketClient.SendAsync(message, token);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            int lengthBytesCount = 2;
            var lengthBuffer = new byte[lengthBytesCount];
            int bytesRead = await stream.ReadAsync(lengthBuffer, 0, lengthBytesCount);
            Assert.Equal(lengthBytesCount, bytesRead);

            string serializedMessage = JsonConvert.SerializeObject(message);
            int readLength = lengthBuffer.ToInt16();
            Assert.Equal(serializedMessage.Length, readLength);

            var buffer = new byte[readLength];
            bytesRead = await stream.ReadAsync(buffer, 0, readLength);
            Assert.Equal(bytesRead, readLength);

            var msgString = Encoding.UTF8.GetString(buffer, 0, readLength);
            Assert.Equal(serializedMessage, msgString);
        }

        [Fact]
        public async void ReceiveAsync_Test()
        {
            // Arrange
            var stream = new MemoryStream(100);
            tcpClientMock.Setup(s => s.Connected).Returns(true);
            tcpClientMock.Setup(s => s.GetStream).Returns(stream);
            var socketClient = new TcpSocketClient<Message, Message>(tcpClient, logger);
            string host = "";
            int port = 0;
            var token = CancellationToken.None;
            await socketClient.ConnectAsync(host, port, token);

            var sentMessage = new Message(MessageID.Unknown, 1, new EmptyAnswerPayload());
            await socketClient.SendAsync(sentMessage, token);
            stream.Seek(0, SeekOrigin.Begin);

            // Act
            (bool wasReceived, Message message) = await socketClient.ReceiveAsync(token);

            // Assert
            Assert.True(wasReceived, "Should receive message");
            Assert.True(message.AreAllPropertiesTheSame(sentMessage),
                "Received message should be the same as sent");
        }
    }
}
