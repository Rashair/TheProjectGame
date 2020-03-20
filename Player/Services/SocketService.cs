using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Player.Clients;
using Shared.Models.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Player.Services
{
    public class SocketService : BackgroundService
    {
        private readonly IConfiguration conf;
        private readonly BufferBlock<GMMessage> queue;
        private readonly ISocketClient<GMMessage, AgentMessage> client;

        // TODO from config
        public Uri ConnectUri => new Uri("ws://localhost:8111/palyer");

        public SocketService(IConfiguration conf, BufferBlock<GMMessage> queue,
            ISocketClient<GMMessage, AgentMessage> client)
        {
            this.conf = conf;
            this.queue = queue;
            this.client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO add reconnect policy
            if (!stoppingToken.IsCancellationRequested)
            {
                await client.ConnectAsync(ConnectUri, stoppingToken);
                await Task.Yield();
                (bool result, GMMessage message) = await client.ReceiveAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested && result)
                {
                    // TODO switch to logger
                    bool sended = await queue.SendAsync(message, stoppingToken);
                    if (!sended)
                        Console.WriteLine($"SocketService| GMMessage id: {message.id} has been lost");
                    (result, message) = await client.ReceiveAsync(stoppingToken);
                }
            }
        }
    }
}
