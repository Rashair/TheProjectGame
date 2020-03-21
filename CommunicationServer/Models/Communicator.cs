using Shared.Models.Messages;
using Shared.Senders;
using System;
using System.Collections.Generic;

namespace CommunicationServer.Models
{
    public class Communicator
    {
        private readonly Dictionary<int, Descriptor> correlation;
        private readonly Descriptor gmDescriptor;
        private readonly ISender messageService;

        public void SendMessageToAgent(GMMessage gmMessage)
        {
            throw new NotImplementedException();
        }

        public void SendMessageToGM(AgentMessage agentMessage)
        {
            throw new NotImplementedException();
        }
    }
}