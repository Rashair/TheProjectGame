using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Serilog;
using Shared.Clients;
using Shared.Managers;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace Shared.Tests.Managers;

public class TcpSocketManagerTests
{
    private readonly ILogger logger;
    private readonly TcpSocketManager<Message, Message> tcpSocketManager;

    public TcpSocketManagerTests()
    {
        logger = MockGenerator.Get<ILogger>();
        tcpSocketManager = new TcpSocketManager<Message, Message>(logger);
    }

    [Fact]
    public void IsOpen_Test()
    {
        // Arrange
        var clientMock = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };

        // Act
        tcpSocketManager.Invoke("IsOpen", new object[] { clientMock.Object });

        // Assert
        clientMock.Verify(m => m.IsOpen, Times.Once(), "IsOpen from client should be checked");
    }

    [Fact]
    public void IsSame_Test()
    {
        // Arrange
        var tcpClient = new TcpClientWrapper();
        var clientMock1 = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };
        clientMock1.Setup(c => c.GetSocket()).Returns(tcpClient);
        var clientMock2 = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };
        clientMock2.Setup(c => c.GetSocket()).Returns(tcpClient);

        // Act
        bool result = tcpSocketManager.Invoke<TcpSocketManager<Message, Message>, bool>(
            "IsSame", new object[] { clientMock1.Object, clientMock2.Object });

        // Assert
        Assert.True(result, "Should return true with same tcpClient");
        clientMock1.Verify(c => c.GetSocket(), Times.Once(), "GetSocket from client1 should be invoked");
        clientMock2.Verify(c => c.GetSocket(), Times.Once(), "GetSocket from client2 should be invoked");
    }

    [Fact]
    public void CloseSocketAsync_Test()
    {
        // Arrange
        var clientMock = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };
        var token = CancellationToken.None;

        // Act
        tcpSocketManager.Invoke("CloseSocketAsync", new object[] { clientMock.Object, token });

        // Assert
        clientMock.Verify(c => c.CloseAsync(token), Times.Once(), "Client should be closed");
    }

    [Fact]
    public void SendMessageAsync_OnClientSuccess_ShouldSuccess_Test()
    {
        // Arrange
        var clientMock = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };
        clientMock.Setup(c => c.IsOpen).Returns(true);
        var message = new Message();
        var token = CancellationToken.None;

        // Act
        var task = tcpSocketManager.Invoke<TcpSocketManager<Message, Message>, Task>("SendMessageAsync",
            new object[] { clientMock.Object, message, token });

        // Assert
        Assert.Null(task.Exception);
        clientMock.Verify(c => c.SendAsync(message, token), Times.Once(),
            "Send from client should be invoked");
    }

    [Fact]
    public void SendMessageAsync_OnClientException_ShouldThrowException_Test()
    {
        // Arrange
        var clientMock = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };
        clientMock.Setup(c => c.IsOpen).Returns(true);
        var message = new Message();
        clientMock.Setup(c => c.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>())).
            Throws(new SocketException());

        // Act
        var task = tcpSocketManager.Invoke<TcpSocketManager<Message, Message>, Task>("SendMessageAsync",
            new object[] { clientMock.Object, message, CancellationToken.None });

        // Assert
        Assert.IsType<SocketException>(task.Exception.InnerException);
        clientMock.Verify(c => c.SendAsync(message, CancellationToken.None), Times.Once(),
            "Send from client should be invoked");
    }
}
