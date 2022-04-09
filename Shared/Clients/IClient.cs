using System.IO;
using System.Threading.Tasks;

namespace Shared.Clients;

public interface IClient
{
    bool Connected { get; }

    string Endpoint { get; }

    Stream GetStream { get; }

    Task ConnectAsync(string host, int port);

    void Close();
}
