using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Newtonsoft.Json;
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
        private readonly ISocketClient<PlayerMessage, GMMessage> socketClient;
        private DateTime lockedTill;
        private AbstractField position;

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

        public GMPlayer(int id, GameConfiguration conf, ISocketClient<PlayerMessage, GMMessage> socketClient, Team team,
            ILogger log, bool isLeader = false)
        {
            logger = log.ForContext<GMPlayer>();
            this.id = id;
            this.conf = conf;
            this.socketClient = socketClient;
            Team = team;
            IsLeader = isLeader;
            lockedTill = DateTime.Now;
        }

        public async Task<bool> MoveAsync(AbstractField field, GM gm, CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                bool moved = field?.MoveHere(this) == true;
                GMMessage message = MoveAnswerMessage(moved, gm);
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
                GMMessage message;
                bool isHolding = !(Holding is null);
                if (isHolding)
                {
                    message = DestructionAnswerMessage();
                    Holding = null;
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
                GMMessage message;
                if (Holding is null)
                {
                    // TODO Issue 129
                    message = UnknownErrorMessage();
                }
                else
                {
                    message = CheckAnswerMessage();
                }

                await SendAndLockAsync(message, conf.CheckPenalty, cancellationToken);
            }
        }

        public async Task DiscoverAsync(GM gm, CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message = DiscoverAnswerMessage(gm);

                await SendAndLockAsync(message, conf.DiscoverPenalty, cancellationToken);
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
                GMMessage message;
                if (Holding != null)
                {
                    (putEvent, wasPieceRemoved) = Holding.Put(Position);
                    message = PutAnswerMessage(putEvent);
                    Holding = null;
                }
                else
                {
                    message = PutErrorMessage(PutError.AgentNotHolding);
                }

                await SendAndLockAsync(message, conf.PutPenalty, cancellationToken);
            }

            return (putEvent, wasPieceRemoved);
        }

        public async Task<(int, bool?)> ForwardKnowledgeReply(PlayerMessage playerMessage, CancellationToken cancellationToken, HashSet<(int recipient, int sender)> legalKnowledgeReplies)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (0, null);
            }

            GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(playerMessage.Payload);
            if (legalKnowledgeReplies.Contains((playerMessage.AgentID, payload.RespondToID)) && isUnlocked)
            {
                legalKnowledgeReplies.Remove((playerMessage.AgentID, payload.RespondToID));
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload()
                {
                    RespondingID = playerMessage.AgentID,
                    Distances = payload.Distances,
                    RedTeamGoalAreaInformations = payload.RedTeamGoalAreaInformations,
                    BlueTeamGoalAreaInformations = payload.BlueTeamGoalAreaInformations,
                };
                GMMessage answer = new GMMessage()
                {
                    MessageID = GMMessageId.GiveInfoForwarded,
                    AgentID = payload.RespondToID,
                    Payload = answerPayload.Serialize(),
                };

                await socketClient.SendAsync(answer, cancellationToken);
                return (playerMessage.AgentID, true);
            }
            else
            {
                return (playerMessage.AgentID, false);
            }
        }

        public async Task<(int, bool?)> ForwardKnowledgeQuestion(PlayerMessage playerMessage, CancellationToken cancellationToken, Dictionary<int, GMPlayer> players, HashSet<(int recipient, int sender)> legalKnowledgeReplies)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (0, null);
            }
            BegForInfoPayload begPayload = JsonConvert.DeserializeObject<BegForInfoPayload>(playerMessage.Payload);
            if (players.ContainsKey(begPayload.AskedAgentID) && isUnlocked)
            {
                BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
                {
                    AskingID = playerMessage.AgentID,
                    Leader = players[playerMessage.AgentID].IsLeader,
                    TeamID = players[playerMessage.AgentID].Team,
                };
                GMMessage gmMessage = new GMMessage()
                {
                    MessageID = GMMessageId.BegForInfoForwarded,
                    AgentID = begPayload.AskedAgentID,
                    Payload = payload.Serialize(),
                };

                legalKnowledgeReplies.Add((begPayload.AskedAgentID, playerMessage.AgentID));
                logger.Verbose(MessageLogger.Sent(gmMessage));
                await socketClient.SendAsync(gmMessage, cancellationToken);
                return (playerMessage.AgentID, true);
            }
            else
            {
                return (playerMessage.AgentID, false);
            }
        }

        public async Task<bool> PickAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryGetLockAsync(cancellationToken);
            bool picked = false;
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message;
                if (Holding is null)
                {
                    picked = Position.PickUp(this);
                    if (picked)
                    {
                        message = PickAnswerMessage();
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
                    GMMessage message = NotWaitedErrorMessage();

                    await socketClient.SendAsync(message, cancellationToken);
                    logger.Verbose(MessageLogger.Sent(message));
                }
                return isUnlocked;
            }
            return false;
        }

        public async Task SendAndLockAsync(GMMessage message, int time, CancellationToken cancellationToken)
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

        private GMMessage NotWaitedErrorMessage()
        {
            NotWaitedErrorPayload payload = new NotWaitedErrorPayload()
            {
                WaitFor = (int)(lockedTill - DateTime.Now).TotalMilliseconds + conf.PrematureRequestPenalty,
            };
            return new GMMessage(GMMessageId.NotWaitedError, id, payload);
        }

        private GMMessage MoveAnswerMessage(bool madeMove, GM gm)
        {
            MoveAnswerPayload payload = new MoveAnswerPayload()
            {
                ClosestPiece = gm.FindClosestPiece(Position),
                CurrentPosition = Position.GetPosition(),
                MadeMove = madeMove,
            };
            return new GMMessage(GMMessageId.MoveAnswer, id, payload);
        }

        private GMMessage UnknownErrorMessage()
        {
            UnknownErrorPayload payload = new UnknownErrorPayload()
            {
                HoldingPiece = !(Holding is null),
                Position = Position.GetPosition(),
            };
            return new GMMessage(GMMessageId.UnknownError, id, payload);
        }

        private GMMessage DestructionAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new GMMessage(GMMessageId.DestructionAnswer, id, payload);
        }

        private GMMessage CheckAnswerMessage()
        {
            CheckAnswerPayload payload = new CheckAnswerPayload()
            {
                Sham = Holding.CheckForSham(),
            };
            return new GMMessage(GMMessageId.CheckAnswer, id, payload);
        }

        private GMMessage DiscoverAnswerMessage(GM gm)
        {
            var discovered = gm.Discover(Position);
            DiscoveryAnswerPayload payload = new DiscoveryAnswerPayload()
            {
                DistanceNW = discovered[Direction.NW],
                DistanceN = discovered[Direction.N],
                DistanceNE = discovered[Direction.NE],
                DistanceW = discovered[Direction.W],
                DistanceFromCurrent = discovered[Direction.FromCurrent],
                DistanceE = discovered[Direction.E],
                DistanceSW = discovered[Direction.SW],
                DistanceS = discovered[Direction.S],
                DistanceSE = discovered[Direction.SE],
            };
            return new GMMessage(GMMessageId.DiscoverAnswer, id, payload);
        }

        private GMMessage PutErrorMessage(PutError error)
        {
            PutErrorPayload payload = new PutErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new GMMessage(GMMessageId.PutError, id, payload);
        }

        private GMMessage PutAnswerMessage(PutEvent putEvent)
        {
            PutAnswerPayload payload = new PutAnswerPayload()
            {
                PutEvent = putEvent,
            };

            return new GMMessage(GMMessageId.PutAnswer, id, payload);
        }

        private GMMessage PickErrorMessage(PickError error)
        {
            PickErrorPayload payload = new PickErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new GMMessage(GMMessageId.PickError, id, payload);
        }

        private GMMessage PickAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new GMMessage(GMMessageId.PickAnswer, id, payload);
        }
    }
}
