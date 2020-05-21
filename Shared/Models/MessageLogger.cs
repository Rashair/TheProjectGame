using System.Runtime.CompilerServices;

using Shared.Converters;
using Shared.Enums;
using Shared.Messages;

namespace Shared.Models
{
    public static class MessageLogger
    {
        public static string Received(this Message msg)
        {
            return $"Received message: {GetDescription(msg)}";
        }

        public static string Sent(this Message msg)
        {
            return $"Sent message: {msg.GetDescription()}";
        }

        public static string GetDescription(this Message msg)
        {
            MessageID messageID = msg.MessageID;
            string messageLog = $"{messageID.GetDescription()}, {msg.AgentID.GetDescription()}, " +
                $"Payload: {{{msg.Payload?.ToString() ?? "null"}}}";

            return messageLog;
        }

        public static string GetDescription<T>(this T value, [CallerMemberName] string name = null)
        {
            return $"{name}: {value}";
        }
    }
}
