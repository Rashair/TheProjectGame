using System;

namespace Shared.Payloads.GMPayloads
{
    public class NotWaitedErrorPayload : Payload
    {
       public DateTime WaitUntil { get; set; }

       public override string ToString()
       {
            return $"WaitUntil:{WaitUntil:HH:mm:ss.fff}";
       }
    }
}
