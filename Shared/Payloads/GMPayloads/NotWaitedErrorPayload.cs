using System;

namespace Shared.Payloads
{
    public class NotWaitedErrorPayload : Payload
    {
       public DateTime WaitUntil { get; set; }
    }
}
