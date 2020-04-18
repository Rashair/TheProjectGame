using Shared.Clients;
using Shared.Messages;

namespace CommunicationServer.Models
{
    public class ServiceShareContainer
    {
        public ISocketClient<GMMessage, PlayerMessage> GMClient { get; set; }
    }
}
