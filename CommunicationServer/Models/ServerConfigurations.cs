namespace CommunicationServer.Models
{
    public class ServerConfigurations
    {
        public int PlayerPort { get; set; }

        public int GMPort { get; set; }

        public string ListenerIP { get; set; }

        public bool Verbose { get; set; }
    }
}
