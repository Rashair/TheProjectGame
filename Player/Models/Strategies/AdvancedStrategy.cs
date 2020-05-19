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
        private readonly RandomGenerator random;
        private readonly Player player;

        // Strategy computed values
        private (int y, int x) previousPosition;
        private int previousDistToPiece;
        private Direction previousDirection;
        private DiscoverState state;

        public AdvancedStrategy(Player player)
        {
            this.player = player;

            this.random = new RandomGenerator();
            this.previousPosition = (-1, -1);
            this.previousDistToPiece = int.MaxValue;
            this.previousDirection = Direction.FromCurrent;
            this.state = DiscoverState.ShouldDiscover;
        }

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
        }

        public Task MakeDecision(CancellationToken cancellationToken)
        {
            CacheCurrentState(cancellationToken);

            Task decision;
            if (!player.HasPiece)
            {
                decision = DoesNotHavePieceDecision();
            }
            else
            {
                decision = HasPieceDecision();
            }

            return decision;
        }

        public Task DoesNotHavePieceDecision()
        {
            Task decision;
            if (isInGoalArea)
            {
                state = DiscoverState.ShouldDiscover;
                decision = DoesNotHavePieceInGoalAreaDecision();
            }
            else if (distToPiece == 0)
            {
                decision = player.Pick(cancellationToken);
                state = DiscoverState.NoAction;
            }
            else if (state == DiscoverState.ShouldDiscover) //// TODO: Trip around square when discoverCost > 8 * moveCost
            {
                decision = player.Discover(cancellationToken);
                state = DiscoverState.Discovered;
            }
            else
            {
                decision = DoesNotHavePieceInTaskAreaMoveDecision();
            }

            previousPosition = player.Position;
            previousDistToPiece = distToPiece;

            return decision;
        }

        private Task DoesNotHavePieceInGoalAreaDecision()
        {
            Direction currentDirection;
            if (previousDistToPiece == 0 || previousPosition != player.Position)
            {
                currentDirection = player.GoalAreaDirection.GetOppositeDirection();
            }
            else
            {
                var directions = GetHorizontalDirections();
                currentDirection = GetRandomDirection(directions);
            }

            previousDirection = currentDirection;

            return player.Move(currentDirection, cancellationToken);
        }

        private Task DoesNotHavePieceInTaskAreaDecision()
        {
            return Task.CompletedTask;
        }

        private Task DoesNotHavePieceInTaskAreaMoveDecision()
        {
            Direction currentDirection = Direction.FromCurrent;
            if (state == DiscoverState.Discovered)
            {
                var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
                currentDirection = directions.Aggregate((d1, d2) =>
                {
                    (int y1, int x1) = d1.GetCoordinates((y, x));
                    (int y2, int x2) = d2.GetCoordinates((y, x));

                    return board[y1, x1].DistToPiece < board[y2, x2].DistToPiece ? d1 : d2;
                });

                state = DiscoverState.NoAction;
            }
            else
            {
                var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
                bool isPreviousDirPossible = directions.Contains(previousDirection);
                if (distToPiece < previousDistToPiece && isPreviousDirPossible)
                {
                    currentDirection = previousDirection;
                }
                else if (!isPreviousDirPossible || distToPiece == previousDistToPiece)
                {
                    var (right, left) = previousDirection.GetPerpendicularDirections();
                    currentDirection = GetRandomDirection(directions, right, left);
                }
                else //// if (currDist > previousDistToPiece)
                {
                    currentDirection = previousDirection.GetOppositeDirection();
                }
            }

            previousDirection = currentDirection;

            return player.Move(currentDirection, cancellationToken);
        }

        private Task HasPieceDecision()
        {
            if (player.IsHeldPieceSham == null)
            {
                return player.CheckPiece(cancellationToken);
            }
            else if (player.IsHeldPieceSham == true)
            {
                return player.DestroyPiece(cancellationToken);
            }
            else if (isInGoalArea)
            {
                return HasPieceInGoalAreaDecision();
            }
            else
            {
                return HasPieceNotInGoalAreaDecision();
            }
        }

        private Task HasPieceInGoalAreaDecision()
        {
            if (board[y, x].GoalInfo == GoalInfo.IDK)
            {
                return player.Put(cancellationToken);
            }

            var directions = GetDirectionsInRange(goalAreaRange.y1, goalAreaRange.y2);

            return player.Move(GetRandomDirection(directions), cancellationToken);
        }

        private Task HasPieceNotInGoalAreaDecision()
        {
            Direction moveDirection;
            if (random.IsLucky(80))
            {
                moveDirection = player.GoalAreaDirection;
            }
            else
            {
                var directions = GetHorizontalDirections();
                moveDirection = GetRandomDirection(directions);
            }

            return player.Move(moveDirection, cancellationToken);
        }

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
                $"lenght: {directions.Count}");
        }
    }
}
