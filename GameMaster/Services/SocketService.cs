using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.PlayerPayloads;

namespace GameMaster.Services
{
    public class SocketService : WaitForInitService
    {
        private const int ConnectRetries = 60;
        private const int RetryIntervalMs = 1000;

        private readonly ISocketClient<PlayerMessage, GMMessage> client;
        private readonly GameConfiguration conf;
        private readonly BufferBlock<PlayerMessage> queue;
        private readonly IApplicationLifetime lifetime;

        public SocketService(GM gameMaster, ISocketClient<PlayerMessage, GMMessage> client, GameConfiguration conf,
            BufferBlock<PlayerMessage> queue, IApplicationLifetime lifetime, ILogger log)
            : base(gameMaster, log.ForContext<GMService>())
        {
            this.client = client;
            this.conf = conf;
            this.queue = queue;
            this.lifetime = lifetime;
        }

        protected override async Task RunService(CancellationToken stoppingToken)
        {
            var (success, errorMessage) = await Helpers.Retry(async () =>
            {
                await client.ConnectAsync(conf.CsIP, conf.CsPort, stoppingToken);
                return true;
            }, ConnectRetries, RetryIntervalMs, stoppingToken);
            if (!success)
            {
                logger.Error($"No connection could be made. Error: {errorMessage}");
                lifetime.StopApplication();
                return;
            }

            (bool receivedMessage, PlayerMessage message) = await client.ReceiveAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested && receivedMessage)
            {
                bool sended = await queue.SendAsync(message, stoppingToken);
                if (!sended)
                {
                    logger.Warning($"SocketService| PlayerMessage id: {message.MessageID} has been lost");
                }
                (receivedMessage, message) = await client.ReceiveAsync(stoppingToken);
            }
            await client.CloseAsync(stoppingToken);

            message = new PlayerMessage()
            {
                AgentID = -1,
                MessageID = PlayerMessageId.CSDisconnected,
                Payload = new EmptyPayload()
            };
            await queue.SendAsync(message, stoppingToken);
        }
    }
}
