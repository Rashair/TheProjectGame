using Shared.Enums;

namespace Shared.Models.Messages
{
    public class GMMessage
    {
        public GMMessageID Id { get; set; }

        public string Payload { get; set; }
    }
}
