using System;
using System.ComponentModel;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace Shared.Converters
{
    public static class ConvertExtensions
    {
        public static MessageID ToMessageIDEnum(this int id)
        {
            if (Enum.IsDefined(typeof(MessageID), id))
            {
                return (MessageID)id;
            }

            throw new InvalidEnumArgumentException($"Wrong MessageID: {id}.");
        }

        public static Type GetPayloadType(this MessageID messageID)
        {
            switch (messageID)
            {
                case MessageID.DestructionAnswer:
                    return typeof(EmptyAnswerPayload);

                case MessageID.CheckAnswer:
                    return typeof(CheckAnswerPayload);

                case MessageID.DiscoverAnswer:
                    return typeof(DiscoveryAnswerPayload);

                case MessageID.EndGame:
                    return typeof(EndGamePayload);

                case MessageID.StartGame:
                    return typeof(StartGamePayload);

                case MessageID.BegForInfoForwarded:
                    return typeof(BegForInfoForwardedPayload);

                case MessageID.JoinTheGameAnswer:
                    return typeof(JoinAnswerPayload);

                case MessageID.MoveAnswer:
                    return typeof(MoveAnswerPayload);

                case MessageID.PutAnswer:
                    return typeof(PutAnswerPayload);

                case MessageID.GiveInfoForwarded:
                    return typeof(GiveInfoForwardedPayload);

                case MessageID.InformationExchangeResponse:
                    return typeof(InformationExchangePayload);

                case MessageID.InformationExchangeRequest:
                    return typeof(InformationExchangePayload);

                case MessageID.NotWaitedError:
                    return typeof(NotWaitedErrorPayload);

                case MessageID.PickError:
                    return typeof(PickErrorPayload);

                case MessageID.PutError:
                    return typeof(PutAnswerPayload);

                case MessageID.UnknownError:
                    return typeof(UnknownErrorPayload);

                case MessageID.CheckPiece:
                case MessageID.PieceDestruction:
                case MessageID.Discover:
                case MessageID.Pick:
                case MessageID.Put:
                    return typeof(EmptyPayload);

                case MessageID.GiveInfo:
                    return typeof(GiveInfoPayload);

                case MessageID.BegForInfo:
                    return typeof(BegForInfoPayload);

                case MessageID.JoinTheGame:
                    return typeof(JoinGamePayload);

                case MessageID.Move:
                    return typeof(MovePayload);
            }

            return typeof(EmptyAnswerPayload);
        }

        public static Payload GetPayload(this MessageID messageID, string json)
        {
            var type = messageID.GetPayloadType();
            return (Payload)JsonConvert.DeserializeObject(json, type);
        }
    }
}
