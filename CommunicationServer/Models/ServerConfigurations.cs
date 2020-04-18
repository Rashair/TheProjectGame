using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationServer.Models
{
    public class ServerConfigurations
    {
        public int PlayerPort { get; set; }

        public int GMPort { get; set; }

        public string ListenerIP { get; set; }
    }
}
