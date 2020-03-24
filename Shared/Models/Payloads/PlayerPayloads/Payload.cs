using Newtonsoft.Json;

namespace Shared.Models.Payloads.PlayerPayload
{
    public abstract class Payload
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
