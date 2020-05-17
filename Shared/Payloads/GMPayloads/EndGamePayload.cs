using Newtonsoft.Json;
using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class EndGamePayload : Payload
    {
        public Team Winner { get; set; }

        public override string ToString()
        {
            return $"Winner:{Winner}";
        }
    }
}
