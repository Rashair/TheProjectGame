using System.IO;
using System.Threading.Tasks;

namespace Shared.Clients
{
    public interface ITcpClient
    {
        bool Connected { get; }

        Stream GetStream();

        Task ConnectAsync(string host, int port);

        void Close();
    }
}
