using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class BegForInfoForwardedPayload : Payload
    {
        public int AskingID { get; set; }

        public bool Leader { get; set; }

        public Team TeamID { get; set; }

        public override string ToString()
        {
            return $"AskingID:{AskingID}, Leader:{Leader}, TeamID{TeamID}";
        }
    }
}
