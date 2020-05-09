using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Payloads.GMPayloads
{
    public class PutAnswerPayload : Payload
    {
        /// <summary>
        /// Null if piece was sham
        /// True if piece was not sham and field was Goal
        /// False if piece was not sham and field was not Goal
        /// </summary>
        public bool? WasGoal { get; set; }

        public override string ToString()
        {
            string message = "";
            if (WasGoal != null)
                message = $"WasGoal:{WasGoal}";
            return message;
        }
    }
}
