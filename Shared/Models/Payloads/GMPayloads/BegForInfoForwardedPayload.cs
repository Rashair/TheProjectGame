namespace Shared.Models.Payloads
{
    public class BegForInfoForwardedPayload : Payload
    {
        public int AskingID { get; set; }

        public bool Leader { get; set; }

        public string TeamId { get; set; }
    }
}
