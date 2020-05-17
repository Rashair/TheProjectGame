using System;

using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class PutAnswerPayload : Payload
    {
        public PutEvent PutEvent { get; set; }

        public override string ToString()
        {
            string message = "";
            message += $" PutEvent: {Enum.GetName(typeof(PutEvent), PutEvent)}";
            return message;
        }
    }
}
