using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace theprojectgame.Models.Communicator
{
    public class Descriptor
    {

    } //klasa nie zawarta w specyfikacji, dodana dla zgodnosci

    public interface ISender
    {
        void SendMessage();
    }

    public class GMMessage
    {
        public int id;
        public string payload;
    }

    public class AgentMessage
    {
        public string payload;
    }

    public class Communicator
    {
        private Dictionary<int, Descriptor> correlation;
        private Descriptor gmDescriptor;
        private ISender messageService;

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