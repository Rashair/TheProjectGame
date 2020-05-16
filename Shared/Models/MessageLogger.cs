using Newtonsoft.Json;
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
                    payload = GetPayload(gmMessage.MessageID, gmMessage.Payload);
                    break;
                case PlayerMessage playerMessage:
                    messageLog = $"MessageId:{playerMessage.MessageID}, AgentID:{playerMessage.AgentID}";
                    payload = GetPayload(playerMessage.MessageID, playerMessage.Payload);
                    break;
            }
            messageLog += $", Payload: {{{payload?.ToString() ?? "null"}}}";
            return messageLog;
        }

        private static Payload GetPayload(GMMessageId messageId, string payload)
        {
            switch (messageId)
            {
                case GMMessageId.DestructionAnswer:
                    return null;

                case GMMessageId.CheckAnswer:
                    return JsonConvert.DeserializeObject<CheckAnswerPayload>(payload);

                case GMMessageId.DiscoverAnswer:
                    return JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(payload);

                case GMMessageId.EndGame:
                    return JsonConvert.DeserializeObject<EndGamePayload>(payload);

                case GMMessageId.StartGame:
                    return JsonConvert.DeserializeObject<StartGamePayload>(payload);

                case GMMessageId.BegForInfoForwarded:
                    return JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(payload);

                case GMMessageId.JoinTheGameAnswer:
                    return JsonConvert.DeserializeObject<JoinAnswerPayload>(payload);

                case GMMessageId.MoveAnswer:
                    return JsonConvert.DeserializeObject<MoveAnswerPayload>(payload);

                case GMMessageId.PutAnswer:
                    return JsonConvert.DeserializeObject<PutAnswerPayload>(payload);

                case GMMessageId.GiveInfoForwarded:
                    return JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(payload);

                case GMMessageId.NotWaitedError:
                    return JsonConvert.DeserializeObject<NotWaitedErrorPayload>(payload);

                case GMMessageId.PickError:
                    return JsonConvert.DeserializeObject<PickErrorPayload>(payload);

                case GMMessageId.PutError:
                    return JsonConvert.DeserializeObject<PutAnswerPayload>(payload);

                case GMMessageId.UnknownError:
                    return JsonConvert.DeserializeObject<UnknownErrorPayload>(payload);
            }

            return null;
        }

        private static Payload GetPayload(PlayerMessageId messageId, string payload)
        {
            switch (messageId)
            {
                case PlayerMessageId.CheckPiece:
                case PlayerMessageId.PieceDestruction:
                case PlayerMessageId.Discover:
                case PlayerMessageId.Pick:
                case PlayerMessageId.Put:
                    return null;

                case PlayerMessageId.GiveInfo:
                    return JsonConvert.DeserializeObject<GiveInfoPayload>(payload);

                case PlayerMessageId.BegForInfo:
                    return JsonConvert.DeserializeObject<BegForInfoPayload>(payload);

                case PlayerMessageId.JoinTheGame:
                    return JsonConvert.DeserializeObject<JoinGamePayload>(payload);

                case PlayerMessageId.Move:
                    return JsonConvert.DeserializeObject<MovePayload>(payload);
            }

            return null;
        }
    }
}
