using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Shared.Clients
{
    public class TcpClientWrapper : IClient
    {
        private readonly TcpClient tcpClient;

        public TcpClientWrapper()
        {
            tcpClient = new TcpClient();
        }

        public TcpClientWrapper(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }

        public bool Connected => tcpClient.Connected;

        public string Endpoint => tcpClient.Client.RemoteEndPoint.ToString();

        public Stream GetStream => tcpClient.GetStream();

        public async Task ConnectAsync(string host, int port)
        {
            await tcpClient.ConnectAsync(host, port);
        }

        public void Close()
        {
            tcpClient.Close();
        }
    }
}
