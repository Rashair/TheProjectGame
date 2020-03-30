using Newtonsoft.Json;

namespace Shared.Payloads
{
    public abstract class Payload
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
