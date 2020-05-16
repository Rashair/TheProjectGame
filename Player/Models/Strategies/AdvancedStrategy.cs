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
            if (y <= player.GoalAreaSize)
            {
                directions.Remove(Direction.S);
            }
            else if (y >= player.BoardSize.y - player.GoalAreaSize - 1)
            {
                directions.Remove(Direction.N);
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
            (int y, int x) = player.Position;
            if (player.IsHeldPieceSham == null)
            {
                return player.CheckPiece(cancellationToken);
            }
            else if (player.IsHeldPieceSham == true)
            {
                return player.DestroyPiece(cancellationToken);
            }

            List<Direction> directions;
            int ind;
            switch (player.Team)
            {
                case Team.Red:
                {
                    if (y < player.GoalAreaSize)
                    {
                        if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
                        {
                            return player.Put(cancellationToken);
                        }

                        directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
                        if (y == player.GoalAreaSize - 1)
                            directions.Remove(Direction.N);
                        if (y == 0)
                            directions.Remove(Direction.S);
                        if (x == 0)
                            directions.Remove(Direction.W);
                        if (x == player.BoardSize.x - 1)
                            directions.Remove(Direction.E);

                        ind = random.Next(directions.Count);

                        return player.Move(directions[ind], cancellationToken);
                    }

                    if (random.Next(1, 6) < 5)
                    {
                        return player.Move(Direction.S, cancellationToken);
                    }

                    directions = new List<Direction>() { Direction.W, Direction.E };
                    if (x == 0)
                        directions.Remove(Direction.W);
                    if (x == player.BoardSize.x - 1)
                        directions.Remove(Direction.E);

                    ind = random.Next(directions.Count);

                    return player.Move(directions[ind], cancellationToken);
                }
                case Team.Blue:
                {
                    int beginning = player.BoardSize.y - player.GoalAreaSize;
                    if (y >= beginning)
                    {
                        if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
                        {
                            return player.Put(cancellationToken);
                        }

                        directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
                        if (y == beginning)
                            directions.Remove(Direction.S);
                        if (y == player.BoardSize.y - 1)
                            directions.Remove(Direction.N);
                        if (x == 0)
                            directions.Remove(Direction.W);
                        if (x == player.BoardSize.x - 1)
                            directions.Remove(Direction.E);

                        ind = random.Next(directions.Count);

                        return player.Move(directions[ind], cancellationToken);
                    }

                    if (random.Next(1, 6) < 5)
                    {
                        return player.Move(Direction.N, cancellationToken);
                    }

                    directions = new List<Direction>() { Direction.W, Direction.E };
                    if (x == 0)
                        directions.Remove(Direction.W);
                    if (x == player.BoardSize.x - 1)
                        directions.Remove(Direction.E);

                    ind = random.Next(directions.Count);

                    return player.Move(directions[ind], cancellationToken);
                }
            }

            return Task.CompletedTask;
        }
    }
}
