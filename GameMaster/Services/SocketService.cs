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

        private readonly ISocketClient<Message, Message> client;
        private readonly GameConfiguration conf;
        private readonly BufferBlock<Message> queue;
        private readonly IHostApplicationLifetime lifetime;

        public SocketService(GM gameMaster, ISocketClient<Message, Message> client, GameConfiguration conf,
            BufferBlock<Message> queue, IHostApplicationLifetime lifetime, ILogger log)
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

            (bool receivedMessage, Message message) = await client.ReceiveAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested && receivedMessage)
            {
                bool sended = await queue.SendAsync(message, stoppingToken);
                if (!sended)
                {
                    logger.Warning($"SocketService| Message id: {message.MessageID} has been lost");
                }
                (receivedMessage, message) = await client.ReceiveAsync(stoppingToken);
            }
            await client.CloseAsync(stoppingToken);

            message = new Message()
            {
                AgentID = -1,
                MessageID = MessageID.CSDisconnected,
                Payload = new EmptyPayload()
            };
            await queue.SendAsync(message, stoppingToken);
        }
    }
}
