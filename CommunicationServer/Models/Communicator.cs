using System;
using System.Collections.Generic;

using Shared.Messages;
using Shared.Senders;

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

        public void SendMessageToGM(PlayerMessage agentMessage)
        {
            throw new NotImplementedException();
        }
    }
}
