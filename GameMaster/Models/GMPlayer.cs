using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Serilog;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;

namespace GameMaster.Models
{
    public class GMPlayer
    {
        private ILogger logger;
        private readonly int id;
        private readonly GameConfiguration conf;
        private readonly ISocketManager<WebSocket, GMMessage> socketManager;
        private int messageCorrelationId;
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

        public int SocketID { get; set; }

        public bool IsLeader { get; set; }

        public Team Team { get; }

        public GMPlayer(int id, GameConfiguration conf, ISocketManager<WebSocket, GMMessage> socketManager, Team team,
            bool isLeader = false)
        {
            logger = Log.ForContext<GMPlayer>();
            this.id = id;
            this.conf = conf;
            this.socketManager = socketManager;
            Team = team;
            IsLeader = isLeader;
            lockedTill = DateTime.Now;
        }

        public bool TryLock(TimeSpan timeSpan)
        {
            DateTime frozenTime = DateTime.Now;
            bool isUnlocked = lockedTill <= frozenTime;
            if (isUnlocked)
            {
                lockedTill = frozenTime + timeSpan;
            }
            return isUnlocked;
        }

        public async Task<bool> MoveAsync(AbstractField field, GM gm, CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.MovePenalty, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                bool moved = field?.MoveHere(this) == true;
                GMMessage message = MoveAnswerMessage(moved, gm);
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
                return moved;
            }
            return false;
        }

        public async Task<bool> DestroyHoldingAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.DestroyPenalty, cancellationToken);
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
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
                return isHolding;
            }
            return false;
        }

        public async Task CheckHoldingAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.CheckPenalty, cancellationToken);
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
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
            }
        }

        public async Task DiscoverAsync(GM gm, CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.DiscoverPenalty, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message = DiscoverAnswerMessage(gm);
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
            }
        }

        /// <returns>
        /// Task<(bool goal, bool removed)>
        /// </returns>
        public async Task<(bool, bool)> PutAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.PutPenalty, cancellationToken);
            (bool goal, bool removed) = (false, false);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message;
                if (Holding is null)
                {
                    message = PutErrorMessage(PutError.AgentNotHolding);
                }
                else
                {
                    (goal, removed) = Holding.PutOnField(Position);
                    message = PutAnswerMessage(goal);
                    Holding = null;
                }
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
            }
            return (goal, removed);
        }

        public async Task<bool> PickAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.PickPenalty, cancellationToken);
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
                await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
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

        private async Task<bool> TryLockAsync(int time, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                int rounded = ((int)Math.Round(time / 10.0)) * 10;
                bool isUnlocked = TryLock(TimeSpan.FromMilliseconds(rounded));
                if (!isUnlocked)
                {
                    GMMessage message = NotWaitedErrorMessage();
                    await socketManager.SendMessageAsync(SocketID, message, cancellationToken);
                }
                return isUnlocked;
            }
            return false;
        }

        private GMMessage NotWaitedErrorMessage()
        {
            NotWaitedErrorPayload payload = new NotWaitedErrorPayload()
            {
                WaitUntil = lockedTill,
            };
            return new GMMessage(GMMessageID.NotWaitedError, payload);
        }

        private GMMessage MoveAnswerMessage(bool madeMove, GM gm)
        {
            MoveAnswerPayload payload = new MoveAnswerPayload()
            {
                ClosestPiece = DiscoverSpecifiedField(gm),
                CurrentPosition = Position.GetPositionObject(),
                MadeMove = madeMove,
            };
            return new GMMessage(GMMessageID.MoveAnswer, payload);
        }

        private int DiscoverSpecifiedField(GM gm)
        {
            if (Position.ContainsPieces() && position.CanPick())
            {
                return 0;
            }

            return gm.FindClosestPiece(Position);
        }

        private GMMessage UnknownErrorMessage()
        {
            UnknownErrorPayload payload = new UnknownErrorPayload()
            {
                HoldingPiece = !(Holding is null),
                Position = Position.GetPositionObject(),
            };
            return new GMMessage(GMMessageID.UnknownError, payload);
        }

        private GMMessage DestructionAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new GMMessage(GMMessageID.DestructionAnswer, payload);
        }

        private GMMessage CheckAnswerMessage()
        {
            CheckAnswerPayload payload = new CheckAnswerPayload()
            {
                Sham = Holding.CheckForSham(),
            };
            return new GMMessage(GMMessageID.CheckAnswer, payload);
        }

        private GMMessage DiscoverAnswerMessage(GM gm)
        {
            var discovered = gm.Discover(Position);
            DiscoveryAnswerPayload payload = new DiscoveryAnswerPayload()
            {
                DistanceNW = discovered.ContainsKey(Direction.NW) ? discovered[Direction.NW] : -1,
                DistanceN = discovered.ContainsKey(Direction.N) ? discovered[Direction.N] : -1,
                DistanceNE = discovered.ContainsKey(Direction.NE) ? discovered[Direction.NE] : -1,
                DistanceW = discovered.ContainsKey(Direction.W) ? discovered[Direction.W] : -1,
                DistanceFromCurrent = discovered[Direction.FromCurrent],
                DistanceE = discovered.ContainsKey(Direction.E) ? discovered[Direction.E] : -1,
                DistanceSW = discovered.ContainsKey(Direction.SW) ? discovered[Direction.SW] : -1,
                DistanceS = discovered.ContainsKey(Direction.S) ? discovered[Direction.S] : -1,
                DistanceSE = discovered.ContainsKey(Direction.SE) ? discovered[Direction.SE] : -1,
            };
            return new GMMessage(GMMessageID.DiscoverAnswer, payload);
        }

        private GMMessage PutErrorMessage(PutError error)
        {
            PutErrorPayload payload = new PutErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new GMMessage(GMMessageID.PutError, payload);
        }

        private GMMessage PutAnswerMessage(bool goal)
        {
            // TODO Issue 119
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new GMMessage(GMMessageID.PutAnswer, payload);
        }

        private GMMessage PickErrorMessage(PickError error)
        {
            PickErrorPayload payload = new PickErrorPayload()
            {
                ErrorSubtype = error,
            };
            return new GMMessage(GMMessageID.PickError, payload);
        }

        private GMMessage PickAnswerMessage()
        {
            EmptyAnswerPayload payload = new EmptyAnswerPayload();
            return new GMMessage(GMMessageID.PickAnswer, payload);
        }
    }
}
