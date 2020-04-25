using System.Collections.Generic;
using System.Linq;

using Moq;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Shared.Clients;
using Shared.Enums;
using Shared.Managers;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace Shared.Tests
{
    public class TcpSocketManagerTests
    {
        private readonly ILogger logger;
        private readonly TcpSocketManager<PlayerMessage, GMMessage> tcpSocketManager;

        public TcpSocketManagerTests()
        {
            logger = MockGenerator.Get<ILogger>();
            tcpSocketManager = new TcpSocketManager<PlayerMessage, GMMessage>(logger);
        }

        [Fact]
        public void IsOpenTest()
        {
            // Arrange
            var clientMock = new Mock<ISocketClient<PlayerMessage, GMMessage>>()
            {
                DefaultValue = DefaultValue.Mock,
            };

            tcpSocketManager.Invoke("IsOpen", new object[] { clientMock.Object });

            clientMock.Verify(m => m.IsOpen, Times.Once(), "IsOpen from client should be checked");
        }
    }
}
