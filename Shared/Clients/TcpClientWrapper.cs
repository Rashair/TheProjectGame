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

        public Stream GetStream()
        {
            return tcpClient.GetStream();
        }

        public async Task ConnectAsync(string host, int port)
        {
            await tcpClient.ConnectAsync(host, port);
        }

        public void Close()
        {
            tcpClient.GetStream().Close();
            tcpClient.Close();
        }
    }
}
