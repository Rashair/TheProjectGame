using Newtonsoft.Json;

namespace Player.Models.Payloads
{
    public abstract class Payload
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
