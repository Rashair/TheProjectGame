using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Messages;
using GameMaster.Models.Payloads;
using GameMaster.Models.Pieces;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace GameMaster.Models
{
    public class GMPlayer
    {
        private readonly ILogger logger;
        private readonly int id;
        private readonly GameConfiguration conf;
        private readonly ISocketClient<Message, Message> socketClient;
        private DateTime lockedTill;
        private AbstractField position;
        private readonly WebSocketManager<ClientMessage> guiManager;

        public AbstractField Position
        {
            get => position;
            set
            {
                if (!(position is null))
                {
                    position.Leave(this);
                }
                position = value;
            }
        }

        public AbstractPiece Holding { get; set; }

        public bool IsLeader { get; set; }

        public Team Team { get; }

        public GMPlayer(int id, GameConfiguration conf, ISocketClient<Message, Message> socketClient, Team team,
            ILogger log, bool isLeader = false, WebSocketManager<ClientMessage> guiManager = null)
        {
            logger = log.ForContext<GMPlayer>();
            this.id = id;
            this.conf = conf;
            this.socketClient = socketClient;
            this.guiManager = guiManager;

            Team = team;
            IsLeader = isLeader;
            lockedTill = DateTime.Now;
        }

        public async Task<bool> MoveAsync(AbstractField field, Func<AbstractField, int?> findClosestPiece,
            CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                bool moved = field?.MoveHere(this) == true;

                if (!(guiManager is null))
                {
                    ClientMessage clientMessage = new ClientMessage
                    {
                        Type = "Move",
                        Payload = new MoveClientPayload
                        {
                            Id = id,
                            X = this[1],
                            Y = this[0]
                        }
                    };
                    await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                }

                int? closestPiece = findClosestPiece(Position);
                Message message = MoveAnswerMessage(moved, closestPiece);
                await SendAndLockAsync(message, conf.MovePenalty, cancellationToken);

                return moved;
            }
            return false;
        }

        public async Task<bool> DestroyHoldingAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                Message message;
                bool isHolding = !(Holding is null);
                if (isHolding)
                {
                    message = DestructionAnswerMessage();
                    Holding = null;

                    if (!(guiManager is null))
                    {
                        ClientMessage clientMessage = new ClientMessage
                        {
                            Type = "Destroy",
                            Payload = new PlayerIdClientPayload
                            {
                                Id = id
                            }
                        };
                        await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                    }
                }
                else
                {
                    // TODO Issue 129
                    message = UnknownErrorMessage();
                }
                await SendAndLockAsync(message, conf.DestroyPenalty, cancellationToken);

                return isHolding;
            }
            return false;
        }

        public async Task CheckHoldingAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                Message message;
                if (Holding is null)
                {
                    // TODO Issue 129
                    message = UnknownErrorMessage();
                }
                else
                {
                    message = CheckAnswerMessage();
                }

                await SendAndLockAsync(message, conf.CheckForShamPenalty, cancellationToken);
            }
        }

        public async Task DiscoverAsync(Func<AbstractField, Dictionary<Direction, int?>> discover,
            CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                var discoverResult = discover(Position);
                Message message = DiscoverAnswerMessage(discoverResult);

                await SendAndLockAsync(message, conf.DiscoveryPenalty, cancellationToken);
            }
        }

        /// <returns>
        /// Task<(PutEvent putEvent, bool wasPieceRemoved)>
        /// </returns>
        public async Task<(PutEvent putEvent, bool wasPieceRemoved)> PutAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            (PutEvent putEvent, bool wasPieceRemoved) = (PutEvent.Unknown, false);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                Message message;
                if (Holding != null)
                {
                    (putEvent, wasPieceRemoved) = Holding.Put(Position);
                    message = PutAnswerMessage(putEvent);
                    Holding = null;

                    if (!(guiManager is null))
                    {
                        string typeGUI;
                        switch (putEvent)
                        {
                            case PutEvent.TaskField:
                                typeGUI = "Put";
                                break;

                            case PutEvent.NormalOnGoalField:
                                typeGUI = "Goal";
                                break;

                            default:
                                typeGUI = "Destroy";
                                break;
                        }

                        ClientMessage clientMessage = new ClientMessage
                        {
                            Type = typeGUI,
                            Payload = new PlayerIdClientPayload
                            {
                                Id = id
                            }
                        };
                        await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                    }
                }
                else
                {
                    message = PutErrorMessage(PutError.AgentNotHolding);
                }

                await SendAndLockAsync(message, conf.PutPenalty, cancellationToken);
            }

            return (putEvent, wasPieceRemoved);
        }

        public async Task<(int, bool?)> ForwardKnowledgeReply(Message playerMessage, CancellationToken cancellationToken, HashSet<(int recipient, int sender)> legalKnowledgeReplies)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (0, null);
            }

            GiveInfoPayload payload = (GiveInfoPayload)playerMessage.Payload;
            int agentID = playerMessage.AgentID.Value;
            if (legalKnowledgeReplies.Contains((agentID, payload.RespondToID)) && isUnlocked)
            {
                legalKnowledgeReplies.Remove((agentID, payload.RespondToID));
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload(conf.Width)
                {
                    RespondingID = agentID,
                    Distances = payload.Distances,
                    RedTeamGoalAreaInformations = payload.RedTeamGoalAreaInformations,
                    BlueTeamGoalAreaInformations = payload.BlueTeamGoalAreaInformations,
                };
                Message answer = new Message()
                {
                    MessageID = MessageID.GiveInfoForwarded,
                    AgentID = payload.RespondToID,
                    Payload = answerPayload,
                };

                await socketClient.SendAsync(answer, cancellationToken);
                return (agentID, true);
            }
            else
            {
                return (agentID, false);
            }
        }

        public async Task<(int, bool?)> ForwardKnowledgeQuestion(Message message, CancellationToken cancellationToken, Dictionary<int, GMPlayer> players, HashSet<(int recipient, int sender)> legalKnowledgeReplies)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (0, null);
            }

            BegForInfoPayload begPayload = (BegForInfoPayload)message.Payload;
            int agentID = message.AgentID.Value;
            if (players.ContainsKey(begPayload.AskedAgentID) && isUnlocked)
            {
                BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
                {
                    AskingID = agentID,
                    Leader = players[agentID].IsLeader,
                    TeamID = players[agentID].Team,
                };
                Message gmMessage = new Message()
                {
                    MessageID = MessageID.BegForInfoForwarded,
                    AgentID = begPayload.AskedAgentID,
                    Payload = payload,
                };

                legalKnowledgeReplies.Add((begPayload.AskedAgentID, agentID));
                logger.Verbose(MessageLogger.Sent(gmMessage));
                await socketClient.SendAsync(gmMessage, cancellationToken);

                return (agentID, true);
            }

            return (agentID, false);
        }

        public async Task<bool> PickAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            bool picked = false;
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                Message message;
                if (Holding is null)
                {
                    picked = Position.PickUp(this);
                    if (picked)
                    {
                        message = PickAnswerMessage();

                        if (!(guiManager is null))
                        {
                            ClientMessage clientMessage = new ClientMessage
                            {
                                Type = "Pick",
                                Payload = new PickClientPayload
                                {
                                    Id = id,
                                    ContainPieces = Position.ContainsPieces()
                                }
                            };
                            await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                        }
                    }
                    else
                    {
                        message = PickErrorMessage(PickError.NothingThere);
                    }
                }
                else
                {
                    message = PickErrorMessage(PickError.Other);
                }
                await SendAndLockAsync(message, conf.PickupPenalty, cancellationToken);
            }

            return picked;
        }

        public int[] GetPosition()
        {
            return Position.GetPosition();
        }

        public int this[int i]
        {
            get { return position.GetPosition()[i]; }
        }

        public async Task<bool> TryGetLockAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var frozenTime = DateTime.Now;
                bool isUnlocked = lockedTill <= frozenTime;
                if (!isUnlocked)
                {
                    Lock(conf.PrematureRequestPenalty, frozenTime);
                    Message message = NotWaitedErrorMessage();

                    await socketClient.SendAsync(message, cancellationToken);
                    logger.Verbose(MessageLogger.Sent(message));
                }
                return isUnlocked;
            }
            return false;
        }

        public async Task SendAndLockAsync(Message message, int time, CancellationToken cancellationToken)
        {
            DateTime frozenTime = DateTime.Now;
            await socketClient.SendAsync(message, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                Lock(time, frozenTime);
                logger.Verbose(MessageLogger.Sent(message));
            }
        }

        private DateTime Lock(int time, DateTime startTime)
        {
            int rounded = ((int)Math.Round(time / 10.0)) * 10;
            TimeSpan span = TimeSpan.FromMilliseconds(rounded);
            if (lockedTill < startTime)
            {
                lockedTill = startTime + span;
            }
            else
            {
                lockedTill += span;
            }

            return lockedTill;
        }

        private Message NotWaitedErrorMessage()
        {
            NotWaitedErrorPayload payload = new NotWaitedErrorPayload()
            {
                WaitFor = (int)(lockedTill - DateTime.Now).TotalMilliseconds,
            };
            return new Message(MessageID.NotWaitedError, id, payload);
        }

        private Message MoveAnswerMessage(bool madeMove, int? closestPiece)
        {
            MoveAnswerPayload payload = new MoveAnswerPayload()
            {
                ClosestPiece = closestPiece,
                CurrentPosition = Position.GetPosition(),
                MadeMove = madeMove,
            };
            return new Message(MessageID.MoveAnswer, id, payload);
        }

        private Message UnknownErrorMessage()
        {
            UnknownErrorPayload payload = new UnknownErrorPayload()
            {
                HoldingPiece = !(Holding is null),
                Position = Position.GetPosition(),
            };
            return new Message(MessageID.UnknownError, id, payload);
        }

        private Message DestructionAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new Message(MessageID.DestructionAnswer, id, payload);
        }

        private Message CheckAnswerMessage()
        {
            CheckAnswerPayload payload = new CheckAnswerPayload()
            {
                Sham = Holding.CheckForSham(),
            };
            return new Message(MessageID.CheckAnswer, id, payload);
        }

        private Message DiscoverAnswerMessage(Dictionary<Direction, int?> discoverResult)
        {
            DiscoveryAnswerPayload payload = new DiscoveryAnswerPayload()
            {
                DistanceNW = discoverResult[Direction.NW],
                DistanceN = discoverResult[Direction.N],
                DistanceNE = discoverResult[Direction.NE],
                DistanceW = discoverResult[Direction.W],
                DistanceFromCurrent = discoverResult[Direction.FromCurrent],
                DistanceE = discoverResult[Direction.E],
                DistanceSW = discoverResult[Direction.SW],
                DistanceS = discoverResult[Direction.S],
                DistanceSE = discoverResult[Direction.SE],
            };
            return new Message(MessageID.DiscoverAnswer, id, payload);
        }

        private Message PutErrorMessage(PutError error)
        {
            PutErrorPayload payload = new PutErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new Message(MessageID.PutError, id, payload);
        }

        private Message PutAnswerMessage(PutEvent putEvent)
        {
            PutAnswerPayload payload = new PutAnswerPayload()
            {
                PutEvent = putEvent,
            };

            return new Message(MessageID.PutAnswer, id, payload);
        }

        private Message PickErrorMessage(PickError error)
        {
            PickErrorPayload payload = new PickErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new Message(MessageID.PickError, id, payload);
        }

        private Message PickAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new Message(MessageID.PickAnswer, id, payload);
        }
    }
}
