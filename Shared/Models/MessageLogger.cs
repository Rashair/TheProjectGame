using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace Shared.Models
{
    public class MessageLogger
    {
        public static string Get(Message message)
        {
            Payload payload = null;
            string logMessage = "";
            switch (message)
            {
                case GMMessage gm:
                    {
                        logMessage = $"MessageId:{gm.Id}, PlayerId:{gm.PlayerId}";
                        logMessage += ", Payload:{";
                        switch (gm.Id)
                        {
                            case GMMessageId.CheckAnswer:
                                payload = JsonConvert.DeserializeObject<CheckAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.DestructionAnswer:
                                break;
                            case GMMessageId.DiscoverAnswer:
                                payload = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.EndGame:
                                payload = JsonConvert.DeserializeObject<EndGamePayload>(gm.Payload);
                                break;
                            case GMMessageId.StartGame:
                                payload = JsonConvert.DeserializeObject<StartGamePayload>(gm.Payload);
                                break;
                            case GMMessageId.BegForInfoForwarded:
                                payload = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(gm.Payload);
                                break;
                            case GMMessageId.JoinTheGameAnswer:
                                payload = JsonConvert.DeserializeObject<JoinAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.MoveAnswer:
                                payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.PutAnswer:
                                payload = JsonConvert.DeserializeObject<PutAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.GiveInfoForwarded:
                                payload = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(gm.Payload);
                                break;
                            case GMMessageId.NotWaitedError:
                                payload = JsonConvert.DeserializeObject<NotWaitedErrorPayload>(gm.Payload);
                                break;
                            case GMMessageId.PickError:
                                payload = JsonConvert.DeserializeObject<PickErrorPayload>(gm.Payload);
                                break;
                            case GMMessageId.PutError:
                                payload = JsonConvert.DeserializeObject<PutAnswerPayload>(gm.Payload);
                                break;
                            case GMMessageId.UnknownError:
                                payload = JsonConvert.DeserializeObject<UnknownErrorPayload>(gm.Payload);
                                break; 
                        }
                        break;
                    }
                case PlayerMessage pm:
                    {
                        logMessage = $"MessageId:{pm.MessageId}, PlayerId:{pm.PlayerId}";
                        logMessage += ", Payload:{";
                        switch (pm.MessageId)
                        {
                            case PlayerMessageId.CheckPiece:
                                break;
                            case PlayerMessageId.PieceDestruction:
                                break;
                            case PlayerMessageId.Discover:
                                break;
                            case PlayerMessageId.GiveInfo:
                                payload = JsonConvert.DeserializeObject<GiveInfoPayload>(pm.Payload);
                                break;
                            case PlayerMessageId.BegForInfo:
                                payload = JsonConvert.DeserializeObject<BegForInfoPayload>(pm.Payload);
                                break;
                            case PlayerMessageId.JoinTheGame:
                                payload = JsonConvert.DeserializeObject<JoinGamePayload>(pm.Payload);
                                break;
                            case PlayerMessageId.Move:
                                payload = JsonConvert.DeserializeObject<MovePayload>(pm.Payload);
                                break;
                            case PlayerMessageId.Pick:
                                break;
                            case PlayerMessageId.Put:
                                break;
                        }
                        break;
                    }
            }
            if (payload == null)
            {
                logMessage += " null";
            }
            else
            {
                logMessage += payload.ToString();
            }
            logMessage += "}";
            return logMessage;
        }
    }
}
