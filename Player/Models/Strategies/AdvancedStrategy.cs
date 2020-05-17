using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;
using Shared.Models;

namespace Player.Models.Strategies
{
    public class AdvancedStrategy : IStrategy
    {
        private const int NumberOfPossibleDirections = 4;
        private readonly Random random = new Random();
        private readonly Player player;

        private (int y, int x) previousPosition;
        private int previousDistToPiece;
        private Direction previousDirection;

        public AdvancedStrategy(Player player)
        {
            this.player = player;

            this.random = new Random();
            this.previousPosition = (-1, -1);
            this.previousDistToPiece = int.MaxValue;
            this.previousDirection = Direction.FromCurrent;
        }

        public Task MakeDecision(CancellationToken cancellationToken)
        {
            Task decision;
            if (!player.HasPiece)
            {
                decision = DoesNotHavePieceDecision(cancellationToken);
            }
            else
            {
                decision = HasPieceDecision(cancellationToken);
            }

            return decision;
        }

        public Task DoesNotHavePieceDecision(CancellationToken cancellationToken)
        {
            if (IsInGoalArea(player.Position.y))
            {
                return DoesNotHavePieceInGoalAreaDecision(cancellationToken);
            }
            else
            {
                return DoesNotHavePieceInTaskAreaDecision(cancellationToken);
            }
        }

        public Task DoesNotHavePieceInGoalAreaDecision(CancellationToken cancellationToken)
        {
            Direction currentDirection;
            if (previousDistToPiece == 0 || previousPosition != player.Position)
            {
                currentDirection = player.GoalAreaDirection.GetOppositeDirection();
            }
            else
            {
                var directions = GetDirectionsInRange(int.MaxValue, int.MinValue);
                currentDirection = GetRandomDirection(directions);
            }

            previousDirection = currentDirection;
            previousPosition = player.Position;

            return player.Move(currentDirection, cancellationToken);
        }

        public Task DoesNotHavePieceInTaskAreaDecision(CancellationToken cancellationToken)
        {
            (int y, int x) = player.Position;
            int distToPiece = player.Board[y, x].DistToPiece;

            Task decision;
            if (distToPiece == 0)
            {
                decision = player.Pick(cancellationToken);
            }
            else
            {
                Direction currentDirection;
                int goalAreaSize = player.GoalAreaSize;
                var directions = GetDirectionsInRange(goalAreaSize, player.BoardSize.y - goalAreaSize);
                if (distToPiece >= previousDistToPiece || !directions.Contains(previousDirection))
                {
                    currentDirection = GetRandomDirection(directions);
                }
                else
                {
                    currentDirection = previousDirection;
                }

                previousDirection = currentDirection;
                decision = player.Move(currentDirection, cancellationToken);
            }

            previousDistToPiece = distToPiece;
            previousPosition = player.Position;

            return decision;
        }

        public Task HasPieceDecision(CancellationToken cancellationToken)
        {
            if (player.IsHeldPieceSham == null)
            {
                return player.CheckPiece(cancellationToken);
            }
            else if (player.IsHeldPieceSham == true)
            {
                return player.DestroyPiece(cancellationToken);
            }
            else if (IsInGoalArea(player.Position.y))
            {
                return HasPieceInGoalAreaDecision(cancellationToken);
            }
            else
            {
                return HasPieceNotInGoalAreaDecision(cancellationToken);
            }
        }

        private Task HasPieceInGoalAreaDecision(CancellationToken cancellationToken)
        {
            (int y, int x) = player.Position;
            if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
            {
                return player.Put(cancellationToken);
            }

            var directions = GetDirectionsInRange(player.GoalAreaRange.y1, player.GoalAreaRange.y2);

            return player.Move(GetRandomDirection(directions), cancellationToken);
        }

        private Task HasPieceNotInGoalAreaDecision(CancellationToken cancellationToken)
        {
            int goalAreaDirectionProbability = 80;
            if (random.Next(101) <= goalAreaDirectionProbability)
            {
                return player.Move(player.GoalAreaDirection, cancellationToken);
            }

            var directions = GetDirectionsInRange(int.MaxValue, int.MinValue);

            return player.Move(GetRandomDirection(directions), cancellationToken);
        }

        private List<Direction> GetDirectionsInRange(int y1 = int.MinValue, int y2 = int.MaxValue)
        {
            List<Direction> directions = new List<Direction>(NumberOfPossibleDirections);
            (int y, int x) = player.Position;
            if (x > 0)
                directions.Add(Direction.W);
            if (x < player.BoardSize.x - 1)
                directions.Add(Direction.E);

            if (y > y1)
                directions.Add(Direction.S);
            if (y < y2 - 1)
                directions.Add(Direction.N);

            return directions;
        }

        private bool IsInGoalArea(int y)
        {
            return y >= player.GoalAreaRange.y1 && y < player.GoalAreaRange.y2;
        }

        private Direction GetRandomDirection(List<Direction> directions)
        {
            return directions[random.Next(directions.Count)];
        }
    }
}
