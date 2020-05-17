using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class AdvancedStrategy : IStrategy
    {
        private const int NumberOfPossibleDirections = 4;
        private readonly Random random = new Random();
        private readonly Player player;

        private int previousDistToPiece;
        private Direction previousDirection;

        public AdvancedStrategy(Player player)
        {
            this.player = player;

            this.random = new Random();
            this.previousDistToPiece = int.MaxValue;
            this.previousDirection = Direction.FromCurrent;
        }

        public Task MakeDecision(CancellationToken cancellationToken)
        {
            if (!player.HasPiece)
            {
                return DoesNotHavePieceDecision(cancellationToken);
            }
            else
            {
                return HasPieceDecision(cancellationToken);
            }
        }

        public Task DoesNotHavePieceDecision(CancellationToken cancellationToken)
        {
            (int y, int x) = player.Position;
            int distToPiece = player.Board[y, x].DistToPiece;
            if (distToPiece == 0)
            {
                return player.Pick(cancellationToken);
            }

            int goalAreaSize = player.GoalAreaSize;
            var directions = GetDirectionsInRange(goalAreaSize, player.BoardSize.y - goalAreaSize);
            if (distToPiece >= previousDistToPiece || !directions.Contains(previousDirection))
            {
                previousDirection = GetRandomDirection(directions);
            }
            previousDistToPiece = distToPiece;

            return player.Move(previousDirection, cancellationToken);
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
