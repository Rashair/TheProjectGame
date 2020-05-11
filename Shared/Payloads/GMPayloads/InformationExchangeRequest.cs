using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Payloads.GMPayloads
{
    public class InformationExchangeRequest
    {
        public bool Received { get; set; }

        public override string ToString()
        {
           return $"Received: {Received}";
        }
    }
}
