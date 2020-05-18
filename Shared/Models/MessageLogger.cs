using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace Shared.Models
{
    public static class MessageLogger
    {
        public static string Received(Message msg)
        {
            return $"Received message: {Get(msg)}";
        }

        public static string Sent(Message msg)
        {
            return $"Sent message: {Get(msg)}";
        }

        public static string Get(Message msg)
        {
            string messageLog = "";
            Payload payload = null;
            switch (msg)
            {
                case GMMessage gmMessage:
                    messageLog = $"MessageId:{gmMessage.MessageID}, AgentID:{gmMessage.AgentID}";
                    payload = GetPayload(gmMessage);
                    break;
                case PlayerMessage playerMessage:
                    messageLog = $"MessageId:{playerMessage.MessageID}, AgentID:{playerMessage.AgentID}";
                    payload = GetPayload(playerMessage);
                    break;
            }
            messageLog += $", Payload: {{{payload?.ToString() ?? "null"}}}";
            return messageLog;
        }

        private static Payload GetPayload(GMMessage message)
        {
            switch (message.MessageID)
            {
                case GMMessageId.DestructionAnswer:
                    return null;

                case GMMessageId.CheckAnswer:
                    return message.DeserializePayload<CheckAnswerPayload>();

                case GMMessageId.DiscoverAnswer:
                    return message.DeserializePayload<DiscoveryAnswerPayload>();

                case GMMessageId.EndGame:
                    return message.DeserializePayload<EndGamePayload>();

                case GMMessageId.StartGame:
                    return message.DeserializePayload<StartGamePayload>();

                case GMMessageId.BegForInfoForwarded:
                    return message.DeserializePayload<BegForInfoForwardedPayload>();

                case GMMessageId.JoinTheGameAnswer:
                    return message.DeserializePayload<JoinAnswerPayload>();

                case GMMessageId.MoveAnswer:
                    return message.DeserializePayload<MoveAnswerPayload>();

                case GMMessageId.PutAnswer:
                    return message.DeserializePayload<PutAnswerPayload>();

                case GMMessageId.GiveInfoForwarded:
                    return message.DeserializePayload<GiveInfoForwardedPayload>();

                case GMMessageId.InformationExchangeResponse:
                    return message.DeserializePayload<InformationExchangePayload>();

                case GMMessageId.InformationExchangeRequest:
                    return message.DeserializePayload<InformationExchangePayload>();

                case GMMessageId.NotWaitedError:
                    return message.DeserializePayload<NotWaitedErrorPayload>();

                case GMMessageId.PickError:
                    return message.DeserializePayload<PickErrorPayload>();

                case GMMessageId.PutError:
                    return message.DeserializePayload<PutAnswerPayload>();

                case GMMessageId.UnknownError:
                    return message.DeserializePayload<UnknownErrorPayload>();
            }

            return null;
        }

        private static Payload GetPayload(PlayerMessage message)
        {
            switch (message.MessageID)
            {
                case PlayerMessageId.CheckPiece:
                case PlayerMessageId.PieceDestruction:
                case PlayerMessageId.Discover:
                case PlayerMessageId.Pick:
                case PlayerMessageId.Put:
                    return null;

                case PlayerMessageId.GiveInfo:
                    return message.DeserializePayload<GiveInfoPayload>();

                case PlayerMessageId.BegForInfo:
                    return message.DeserializePayload<BegForInfoPayload>();

                case PlayerMessageId.JoinTheGame:
                    return message.DeserializePayload<JoinGamePayload>();

                case PlayerMessageId.Move:
                    return message.DeserializePayload<MovePayload>();
            }

            return null;
        }
    }
}
