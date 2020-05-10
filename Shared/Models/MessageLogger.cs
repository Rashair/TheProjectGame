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
            switch (message)
            {
                case GMMessage gm:
                    {
                        string logMessage = $"MessageId:{gm.Id}, PlayerId:{gm.PlayerId}";
                        logMessage += ", Payload:{";
                        switch (gm.Id)
                        {
                            case GMMessageId.CheckAnswer:
                                {
                                    CheckAnswerPayload payload = JsonConvert.DeserializeObject<CheckAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.DestructionAnswer:
                                {
                                    logMessage += " null";
                                    break;
                                }
                            case GMMessageId.DiscoverAnswer:
                                {
                                    DiscoveryAnswerPayload payload = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.EndGame:
                                {
                                    EndGamePayload payload = JsonConvert.DeserializeObject<EndGamePayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.StartGame:
                                {
                                    StartGamePayload payload = JsonConvert.DeserializeObject<StartGamePayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.BegForInfoForwarded:
                                {
                                    BegForInfoForwardedPayload payload = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.JoinTheGameAnswer:
                                {
                                    JoinAnswerPayload payload = JsonConvert.DeserializeObject<JoinAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.MoveAnswer:
                                {
                                    MoveAnswerPayload payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.PutAnswer:
                                {
                                    PutAnswerPayload payload = JsonConvert.DeserializeObject<PutAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.GiveInfoForwarded:
                                {
                                    GiveInfoForwardedPayload payload = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.NotWaitedError:
                                {
                                    NotWaitedErrorPayload payload = JsonConvert.DeserializeObject<NotWaitedErrorPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.PickError:
                                {
                                    PickErrorPayload payload = JsonConvert.DeserializeObject<PickErrorPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.PutError:
                                {
                                    PutAnswerPayload payload = JsonConvert.DeserializeObject<PutAnswerPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case GMMessageId.UnknownError:
                                {
                                    UnknownErrorPayload payload = JsonConvert.DeserializeObject<UnknownErrorPayload>(gm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }  
                        }
                        logMessage += "}";
                        return logMessage;
                    }
                case PlayerMessage pm:
                    {
                        string logMessage = $"MessageId:{pm.MessageId}, PlayerId:{pm.PlayerId}";
                        logMessage += ", Payload:{";
                        switch (pm.MessageId)
                        {
                            case PlayerMessageId.CheckPiece:
                                logMessage += " null";
                                break;
                            case PlayerMessageId.PieceDestruction:
                                logMessage += " null";
                                break;
                            case PlayerMessageId.Discover:
                                logMessage += " null";
                                break;
                            case PlayerMessageId.GiveInfo:
                                {
                                    GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(pm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case PlayerMessageId.BegForInfo:
                                {
                                    BegForInfoPayload payload = JsonConvert.DeserializeObject<BegForInfoPayload>(pm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case PlayerMessageId.JoinTheGame:
                                {
                                    JoinGamePayload payload = JsonConvert.DeserializeObject<JoinGamePayload>(pm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case PlayerMessageId.Move:
                                {
                                    MovePayload payload = JsonConvert.DeserializeObject<MovePayload>(pm.Payload);
                                    if (payload == null)
                                    {
                                        logMessage += " null";
                                    }
                                    else
                                    {
                                        logMessage += payload.ToString();
                                    }
                                    break;
                                }
                            case PlayerMessageId.Pick:
                                logMessage += " null";
                                break;
                            case PlayerMessageId.Put:
                                logMessage += " null";
                                break;
                        }
                        logMessage += "}";
                        return logMessage;
                    }
            }
            return "";
        }
    }
}
