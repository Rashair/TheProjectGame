using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class AdvancedStrategy : IStrategy
    {
        private readonly Random random = new Random();
        private readonly Player player;

        public AdvancedStrategy(Player player)
        {
            this.player = player;
            this.random = new Random();
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
            if (player.Board[y, x].DistToPiece == 0)
            {
                return player.Pick(cancellationToken);
            }

            List<Direction> directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
            if (IsInGoalArea(y))
            {
                directions.Remove(player.GoalAreaDirection);
            }

            if (x == player.BoardSize.x - 1)
            {
                directions.Remove(Direction.E);
            }
            else if (x == 0)
            {
                directions.Remove(Direction.W);
            }

            int ind = random.Next(directions.Count);
            return player.Move(directions[ind], cancellationToken);
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

            List<Direction> directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
            (int y, int x) = player.Position;
            if (y == player.GoalAreaRange.y1)
                directions.Remove(Direction.S);
            if (y == player.GoalAreaRange.y2 - 1)
                directions.Remove(Direction.N);
            if (x == 0)
                directions.Remove(Direction.W);
            if (x == player.BoardSize.x - 1)
                directions.Remove(Direction.E);

            if (IsInGoalArea(y))
            {
                if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
                {
                    return player.Put(cancellationToken);
                }

                return player.Move(GetRandomDirection(directions), cancellationToken);
            }

            if (random.Next(1, 6) < 5)
            {
                return player.Move(player.GoalAreaDirection, cancellationToken);
            }

            directions = new List<Direction>() { Direction.W, Direction.E };
            if (x == 0)
                directions.Remove(Direction.W);
            if (x == player.BoardSize.x - 1)
                directions.Remove(Direction.E);

            return player.Move(GetRandomDirection(directions), cancellationToken);
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
