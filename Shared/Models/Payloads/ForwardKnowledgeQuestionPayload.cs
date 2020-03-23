using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Payloads
{
    public class ForwardKnowledgeQuestionPayload : Payload
    {
        public int askingID;
        public bool leader;
        public Team teamId;
    }
}
