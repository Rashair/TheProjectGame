using Shared.Clients;
using Shared.Messages;

namespace CommunicationServer.Models
{
    public class ServiceShareContainer
    {
        public ISocketClient<Message, Message> GMClient { get; set; }
    }
}
