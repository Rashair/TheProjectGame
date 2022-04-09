using System.Collections.Generic;

using Shared.Clients;
using Shared.Messages;

namespace CommunicationServer.Models;

public class ServiceShareContainer
{
    public ISocketClient<Message, Message> GMClient { get; set; }

    public Dictionary<int, bool> ConfirmedAgents { get; set; }

    public bool GameStarted { get; set; }
}
