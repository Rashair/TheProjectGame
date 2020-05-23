using GameMaster.Models.Payloads;

namespace GameMaster.Models.Messages
{
    public class ClientMessage
    {
        public string Type { get; set; }

        public ClientPayload Payload { get; set; }

        public ClientMessage()
        {
        }

        public ClientMessage(string type, ClientPayload payload = null)
        {
            this.Type = type;
            this.Payload = payload;
        }
    }
}
