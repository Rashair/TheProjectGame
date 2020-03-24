using Newtonsoft.Json;

namespace Shared.Models.Payloads
{
    public abstract class Payload
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
