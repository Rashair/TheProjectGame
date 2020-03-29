using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class Strategy : IStrategy
    {
        private readonly Random random = new Random();

        public async Task MakeDecision(Player player, CancellationToken cancellationToken)
        {
            (int y, int x) = player.Position;
            if (!player.HasPiece)
            {
                if (player.Board[y, x].DistToPiece == 0)
                {
                    await player.Pick(cancellationToken);
                    return;
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
                await player.Move(directions[ind], cancellationToken);
            }
            else
            {
                if (player.IsHeldPieceSham == null)
                {
                    await player.CheckPiece(cancellationToken);
                    return;
                }
                else if (player.IsHeldPieceSham == false)
                {
                    await player.DestroyPiece(cancellationToken);
                    return;
                }

                switch (player.Team)
                {
                    case Team.Red:
                    {
                        if (y < player.GoalAreaSize)
                        {
                            if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
                            {
                                await player.Put(cancellationToken);
                            }
                            else
                            {
                                List<Direction> directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
                                if (y == player.GoalAreaSize - 1)
                                    directions.Remove(Direction.N);
                                if (y == 0)
                                    directions.Remove(Direction.S);
                                if (x == 0)
                                    directions.Remove(Direction.W);
                                if (x == player.BoardSize.x - 1)
                                    directions.Remove(Direction.E);

                                int ind = random.Next(directions.Count);
                                await player.Move(directions[ind], cancellationToken);
                            }
                            return;
                        }

                        if (random.Next(1, 6) < 5)
                        {
                            await player.Move(Direction.S, cancellationToken);
                        }
                        else
                        {
                            List<Direction> directions = new List<Direction>() { Direction.S, Direction.E };
                            if (x == 0)
                                directions.Remove(Direction.W);
                            if (x == player.BoardSize.x - 1)
                                directions.Remove(Direction.E);

                            int ind = random.Next(directions.Count);
                            await player.Move(directions[ind], cancellationToken);
                        }
                        break;
                    }
                    case Team.Blue:
                    {
                        int beginning = player.BoardSize.y - player.GoalAreaSize;
                        if (y >= beginning)
                        {
                            if (player.Board[y, x].GoalInfo == GoalInfo.IDK)
                            {
                                await player.Put(cancellationToken);
                            }
                            else
                            {
                                List<Direction> directions = new List<Direction>() { Direction.N, Direction.S, Direction.E, Direction.W };
                                if (y == beginning)
                                    directions.Remove(Direction.S);
                                if (y == player.BoardSize.y - 1)
                                    directions.Remove(Direction.N);
                                if (x == 0)
                                    directions.Remove(Direction.W);
                                if (x == player.BoardSize.x - 1)
                                    directions.Remove(Direction.E);

                                int ind = random.Next(directions.Count);
                                await player.Move(directions[ind], cancellationToken);
                            }
                            return;
                        }

                        if (random.Next(1, 6) < 5)
                        {
                            await player.Move(Direction.N, cancellationToken);
                        }
                        else
                        {
                            List<Direction> directions = new List<Direction>() { Direction.S, Direction.E };
                            if (x == 0)
                                directions.Remove(Direction.W);
                            if (x == player.BoardSize.x - 1)
                                directions.Remove(Direction.E);

                            int ind = random.Next(directions.Count);
                            await player.Move(directions[ind], cancellationToken);
                        }
                        break;
                    }
                }
            }
        }
    }
}
