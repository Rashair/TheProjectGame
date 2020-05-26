using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Player.Models.Strategies.Utils;
using Shared.Enums;

namespace Player.Models.Strategies
{
    public class AdvancedStrategy : IStrategy
    {
        private const int NumberOfPossibleDirections = 4;
        private const int ALotOfFalseMoves = 10;
        private const int MaxDirectionsHistoryCount = 100;
        private readonly RandomGenerator random;
        private readonly Player player;

        // Strategy computed values
        private (int y, int x) previousPosition;
        private int previousDistToPiece;
        private LinkedList<Direction> directionsHistory;
        private DiscoverState state;
        private LastAction lastAction;

        public AdvancedStrategy(Player player)
        {
            this.player = player;

            this.random = new RandomGenerator();
            this.previousPosition = (-1, -1);
            this.previousDistToPiece = int.MaxValue;
            this.directionsHistory = new LinkedList<Direction>();
            directionsHistory.AddLast(Direction.FromCurrent);

            this.state = DiscoverState.ShouldDiscover;
            this.lastAction = LastAction.None;
        }

        private Direction PreviousDir => directionsHistory.Last.Value;

        // Contstant
        private Field[,] board;
        private (int y, int x) boardSize;
        private int goalAreaSize;
        private (int y1, int y2) goalAreaRange;

        // Changing
        private CancellationToken cancellationToken;
        private int y;
        private int x;
        private int distToPiece;
        private bool isInGoalArea;
        private bool isNotStuckOnPreviousPosition;
        private bool isReallyStuck;

        private void CacheCurrentState(CancellationToken token)
        {
            if (board == null)
            {
                board = player.Board;
                boardSize = player.BoardSize;
                goalAreaSize = player.GoalAreaSize;
                goalAreaRange = player.GoalAreaRange;
            }

            cancellationToken = token;
            (y, x) = player.Position;
            distToPiece = board[y, x].DistToPiece;

            isInGoalArea = y >= goalAreaRange.y1 && y < goalAreaRange.y2;

            isNotStuckOnPreviousPosition = lastAction != LastAction.Move ||
                (previousPosition.y != y || previousPosition.x != x);

            isReallyStuck = !isNotStuckOnPreviousPosition && player.NotMadeMoveInRow > ALotOfFalseMoves;
        }

        public Task MakeDecision(CancellationToken cancellationToken)
        {
            CacheCurrentState(cancellationToken);

            Task decision;
            if (isReallyStuck)
            {
                decision = IsReallyStuck();
            }
            else
            {
                decision = IsNotReallyStuck();
            }

            previousPosition = player.Position;
            previousDistToPiece = distToPiece;

            return decision;
        }

        private Task IsReallyStuck()
        {
            var directions = GetDirectionsInRange();
            var prevDirNode = directionsHistory.Last;
            directions.Remove(prevDirNode.Value);
            directions.Remove(prevDirNode.Previous.Value);

            return MoveToDirection(GetRandomDirection(directions));
        }

        private Task IsNotReallyStuck()
        {
            if (player.HasPiece)
            {
                return HasPiece();
            }
            else
            {
                return NoPiece();
            }
        }

        public Task NoPiece()
        {
            Task decision;
            if (isInGoalArea)
            {
                decision = NoPieceInGoalArea();
                state = DiscoverState.ShouldDiscover;
            }
            else
            {
                decision = NoPieceInTaskArea();
            }

            return decision;
        }

        private Task NoPieceInGoalArea()
        {
            Direction currentDirection;
            bool hadPiece = lastAction == LastAction.Put || lastAction == LastAction.Destroy;
            if (hadPiece || isNotStuckOnPreviousPosition)
            {
                currentDirection = player.GoalAreaDirection.GetOppositeDirection();
            }
            else
            {
                var directions = GetHorizontalDirections();
                currentDirection = GetRandomDirection(directions);
            }

            return MoveToDirection(currentDirection);
        }

        private Task NoPieceInTaskArea()
        {
            Task decision;
            if (distToPiece == 0)
            {
                decision = player.Pick(cancellationToken);
                state = DiscoverState.NoAction;
                lastAction = LastAction.Pick;
            }
            else if (state == DiscoverState.ShouldDiscover ||
                (state != DiscoverState.Discovered && PreviousDir == Direction.FromCurrent))
            {
                //// TODO: Trip around square when discoverCost > 8 * moveCost
                decision = player.Discover(cancellationToken);
                state = DiscoverState.Discovered;
                lastAction = LastAction.Discover;
            }
            else
            {
                decision = NoPieceInTaskAreaMove();
            }

            return decision;
        }

        private Task NoPieceInTaskAreaMove()
        {
            if (state == DiscoverState.Discovered)
            {
                return NoPieceInTaskAreaDiscovered();
            }
            else
            {
                return NoPieceInTaskAreaNotDiscovered();
            }
        }

        private Task NoPieceInTaskAreaDiscovered()
        {
            var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
            var moveDir = directions.Aggregate((d1, d2) =>
            {
                (int y1, int x1) = d1.GetCoordinates((y, x));
                (int y2, int x2) = d2.GetCoordinates((y, x));

                return board[y1, x1].DistToPiece < board[y2, x2].DistToPiece ? d1 : d2;
            });

            state = DiscoverState.NoAction;
            return MoveToDirection(moveDir);
        }

        private Task NoPieceInTaskAreaNotDiscovered()
        {
            Direction currentDirection;
            var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
            bool isPreviousDirPossible = directions.Contains(PreviousDir);
            if (distToPiece < previousDistToPiece && isPreviousDirPossible && isNotStuckOnPreviousPosition)
            {
                currentDirection = PreviousDir;
            }
            else if (distToPiece > previousDistToPiece)
            {
                // Parallel piece
                if (directionsHistory.Count >= 2 || directionsHistory.Last.Previous.Value == PreviousDir)
                {
                    state = DiscoverState.ShouldDiscover;
                }
                currentDirection = PreviousDir.GetOppositeDirection();
            }
            else
            {
                var (right, left) = PreviousDir.GetPerpendicularDirections();
                currentDirection = GetRandomDirection(directions, right, left);
            }

            return MoveToDirection(currentDirection);
        }

        private Task HasPiece()
        {
            if (player.IsHeldPieceSham == null)
            {
                lastAction = LastAction.Check;
                return player.CheckPiece(cancellationToken);
            }
            else if (player.IsHeldPieceSham == true)
            {
                lastAction = LastAction.Destroy;
                return player.DestroyPiece(cancellationToken);
            }
            else if (isInGoalArea)
            {
                return HasPieceInGoalArea();
            }
            else
            {
                return HasPieceInTaskArea();
            }
        }

        private Task HasPieceInGoalArea()
        {
            if (board[y, x].GoalInfo == GoalInfo.IDK)
            {
                lastAction = LastAction.Put;
                return player.Put(cancellationToken);
            }

            var directions = GetDirectionsInRange(goalAreaRange.y1, goalAreaRange.y2);
            Direction currentDir;
            if (!isNotStuckOnPreviousPosition)
            {
                directions.Remove(PreviousDir);
                currentDir = GetRandomDirection(directions);
            }
            else
            {
                currentDir = PreviousDir;
            }

            return MoveToDirection(currentDir);
        }

        private Task HasPieceInTaskArea()
        {
            Direction moveDirection;
            if (isNotStuckOnPreviousPosition || PreviousDir != player.GoalAreaDirection)
            {
                moveDirection = player.GoalAreaDirection;
            }
            else
            {
                moveDirection = GetHorizontalDirectionWhenStuck();
            }

            return MoveToDirection(moveDirection);
        }

        //// Utilites
        //// -------------------------------------------------------------------------------------------

        private List<Direction> GetDirectionsInRange(int y1 = int.MinValue, int y2 = int.MaxValue)
        {
            List<Direction> directions = new List<Direction>(NumberOfPossibleDirections);
            if (x > 0)
                directions.Add(Direction.W);
            if (x < boardSize.x - 1)
                directions.Add(Direction.E);

            if (y > y1)
                directions.Add(Direction.S);
            if (y < y2 - 1)
                directions.Add(Direction.N);

            return directions;
        }

        private List<Direction> GetHorizontalDirections()
        {
            List<Direction> directions = new List<Direction>(NumberOfPossibleDirections / 2);
            if (x > 0)
                directions.Add(Direction.W);
            if (x < boardSize.x - 1)
                directions.Add(Direction.E);

            return directions;
        }

        private Direction GetRandomDirection(List<Direction> directions)
        {
            return directions[random[directions.Count]];
        }

        private Direction GetRandomDirection(List<Direction> directions, Direction right, Direction left)
        {
            int startInd = random[directions.Count];
            int ind = startInd;
            do
            {
                if (directions[ind] == right)
                {
                    return right;
                }
                else if (directions[ind] == left)
                {
                    return left;
                }

                ++ind;
                if (ind == directions.Count)
                {
                    ind = 0;
                }
            }
            while (ind != startInd);

            throw new InvalidOperationException($"None of the provided directions were in list, " +
                $"length: {directions.Count}");
        }

        private Task MoveToDirection(Direction dir)
        {
            lastAction = LastAction.Move;
            directionsHistory.AddLast(dir);
            if (directionsHistory.Count > MaxDirectionsHistoryCount)
            {
                directionsHistory.RemoveFirst();
            }

            return player.Move(dir, cancellationToken);
        }

        private Direction GetHorizontalDirectionWhenStuck()
        {
            var directions = GetHorizontalDirections();
            var prevDir = PreviousDir;
            if (directions.Count > 1)
            {
                if (prevDir == directions[0])
                {
                    return directions[1];
                }
                else if (prevDir == directions[1])
                {
                    return directions[0];
                }
                else
                {
                    return GetRandomDirection(directions);
                }
            }

            return directions[0];
        }
    }
}
