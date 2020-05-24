using System.Runtime.CompilerServices;

using Shared.Messages;

namespace Shared.Models
{
    public static class MessageLogger
    {
        public static string Received(this Message msg)
        {
            return $"Received message: {msg.GetDescription()}";
        }

        public static string Sent(this Message msg)
        {
            return $"Sent message: {msg.GetDescription()}";
        }

        public static string GetDescription(this Message msg)
        {
            string messageLog = $"{msg.MessageID.GetDescription("MessageID")}, {msg.AgentID.GetDescription("AgentID")}, " +
                $"Payload: {{{msg.Payload?.ToString() ?? "null"}}}";

            return messageLog;
        }

        public static string GetDescription<T>(this T value, [CallerMemberName] string name = null)
        {
            return $"{name}: {value}";
        }
    }
}
