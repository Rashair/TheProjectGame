﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Serilog;

namespace Shared.Clients
{
    public class TcpSocketClient<R, S> : ISocketClient<R, S>
    {
        private readonly ILogger logger;
        private readonly TcpClient client;
        private NetworkStream stream;
        private bool isOpen;

        public TcpSocketClient(ILogger log)
        {
            this.logger = log.ForContext<TcpSocketClient<R, S>>();
            this.client = new TcpClient();
        }

        public TcpSocketClient(TcpClient tcpClient, ILogger log)
        {
            logger = log.ForContext<TcpSocketClient<R, S>>();
            client = tcpClient;
            stream = tcpClient.GetStream();
            isOpen = tcpClient.Connected;
        }

        public bool IsOpen => isOpen && client.Connected;

        public object GetSocket()
        {
            return client;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            isOpen = false;
            stream.Close();
            client.Close();
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(host, port);
            stream = client.GetStream();
            isOpen = true;
            logger.Information($"Connected to {host}:{port}");
        }

        public async Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen)
            {
                return (false, default);
            }

            byte[] lengthEndian = new byte[2];
            int countRead = await stream.ReadAsync(lengthEndian, 0, 2, cancellationToken);
            if (cancellationToken.IsCancellationRequested || (countRead == 0) || (countRead == 1))
            {
                if (countRead == 0)
                {
                    logger.Warning("End of stream");
                }
                else if (countRead == 1)
                {
                    logger.Warning("Message length should be wrote on 2 bytes.\n");
                }
                return (false, default);
            }

            int length = TryConvertToInt(lengthEndian);
            if (length == 0)
            {
                logger.Warning("Bad message length.\n");
                return (false, default);
            }

            byte[] buffer = new byte[length];
            countRead = await stream.ReadAsync(buffer, 0, length, cancellationToken);
            if (cancellationToken.IsCancellationRequested || (countRead == 0) || (countRead != length))
            {
                if (countRead == 0)
                {
                    logger.Warning("End of stream");
                }
                else if (countRead != length)
                {
                    logger.Warning("Unexpected message - wrong length provided.\n");
                }
                return (false, default);
            }

            string json = Encoding.UTF8.GetString(buffer, 0, length);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

        private int TryConvertToInt(byte[] lengthEndian)
        {
            try
            {
                return lengthEndian.ToInt16();
            }
            catch (Exception e)
            {
                logger.Warning($"Cannot convert to little-endian.\n{e}");
                throw;
            }
        }

        public async Task SendToAllAsync(List<S> messages, CancellationToken cancellationToken)
        {
            await Task.WhenAll(from message in messages select SendAsync(message, cancellationToken));
        }

        public async Task SendAsync(S message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && IsOpen)
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                byte[] length = buffer.Length.ToLittleEndian();
                await stream.WriteAsync(length, 0, 2, cancellationToken);
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
        }
    }
}