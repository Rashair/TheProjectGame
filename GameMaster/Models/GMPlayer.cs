﻿using System;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;

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
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + MessageLogger.Get(message));
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
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + MessageLogger.Get(message));
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
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + MessageLogger.Get(message));
            }
        }

        public async Task DiscoverAsync(GM gm, CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.DiscoverPenalty, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message = DiscoverAnswerMessage(gm);
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + MessageLogger.Get(message));
            }
        }

        /// <returns>
        /// Task<(bool? goal, bool removed)>
        /// </returns>
        public async Task<(bool?, bool)> PutAsync(CancellationToken cancellationToken)
        {
            bool isUnlocked = await TryLockAsync(conf.PutPenalty, cancellationToken);
            (PutEvent putEvent, bool removed) = (PutEvent.TaskField, false);
            bool? goal = null;
            if (!cancellationToken.IsCancellationRequested && isUnlocked)
            {
                GMMessage message;
                if (Holding is null)
                {
                    message = PutErrorMessage(PutError.AgentNotHolding);
                }
                else
                {
                    (putEvent, removed) = Holding.Put(Position);

                    if (putEvent == PutEvent.NormalOnGoalField)
                    {
                        goal = true;
                    }
                    else if (putEvent == PutEvent.NormalOnNonGoalField)
                    {
                        goal = false;
                    }
                    else
                    {
                        goal = null;
                    }

                    message = PutAnswerMessage(goal, putEvent);
                    Holding = null;
                }
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + MessageLogger.Get(message));
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
                await socketClient.SendAsync(message, cancellationToken);
                logger.Verbose("Sent message." + message.ToString());
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
                    await socketClient.SendAsync(message, cancellationToken);
                    logger.Verbose("Sent message." + MessageLogger.Get(message));
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

        private GMMessage PutAnswerMessage(bool? wasGoal, PutEvent putEvent)
        {
            PutAnswerPayload payload = new PutAnswerPayload()
            {
                WasGoal = wasGoal,
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
