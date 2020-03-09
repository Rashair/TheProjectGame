using System;
using System.Collections.Generic;
using TheProjectGame.Models.Communicator.Messages;
using TheProjectGame.Models.Shared.Senders;

namespace TheProjectGame.Models.Communicator
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