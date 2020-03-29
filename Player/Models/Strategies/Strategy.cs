using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class Strategy : IStrategy
    {
        public async Task MakeDecision(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            if (!player.HavePiece)
            {
                await player.Discover(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await player.AcceptMessage(cancellationToken);

                    (int x, int y) = player.Position;
                    (Direction, int, int)[] neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter((x, y));
                    int[] dist = new int[neighbourCoordinates.Length];

                    for (int i = 0; i < neighbourCoordinates.Length; i++)
                    {
                        dist[i] = player.Board[neighbourCoordinates[i].Item2, neighbourCoordinates[i].Item3].DistToPiece;
                    }
                    Array.Sort(dist, neighbourCoordinates);

                    await player.Move(neighbourCoordinates[0].Item1, cancellationToken);
                    for (int i = 1; i < dist.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        await player.Move(neighbourCoordinates[i].Item1, cancellationToken);
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(cancellationToken).Wait();
                    }
                }
            }
            if (player.HavePiece)
            {
                switch (team)
                {
                    case Team.Red:
                        {
                            if (player.Position.Item2 <= player.BoardSize.y - goalAreaSize)
                            {
                                await player.Move(Direction.N, cancellationToken);
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await player.AcceptMessage(cancellationToken);
                                }
                            }
                            else
                            {
                                RedPlayerMoveToGoalAsync(player, team, goalAreaSize, cancellationToken);
                            }
                            break;
                        }
                    case Team.Blue:
                        {
                            if (player.Position.Item2 >= goalAreaSize)
                            {
                                await player.Move(Direction.S, cancellationToken);
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await player.AcceptMessage(cancellationToken);
                                }
                            }
                            else
                            {
                                BluePlayerMoveToGoalAsync(player, team, goalAreaSize, cancellationToken);
                            }
                            break;
                        }
                }
            }
        }

        public async System.Threading.Tasks.Task RedPlayerMoveToGoalAsync(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            while (player.Position.Item2 < player.BoardSize.y)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    await player.Put(cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await player.AcceptMessage(cancellationToken);
                    }
                    break;
                }
                else
                {
                    Direction[] directions = { Direction.N, Direction.W, Direction.E, Direction.S };
                    await player.Move(Direction.S, cancellationToken);
                    for (int i = 1; i < directions.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        await player.Move(directions[i], cancellationToken);
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await player.AcceptMessage(cancellationToken);
                    }
                }
            }
        }

        public async System.Threading.Tasks.Task BluePlayerMoveToGoalAsync(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            while (player.Position.Item2 >= 0)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    await player.Put(cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await player.AcceptMessage(cancellationToken);
                    }
                    break;
                }
                else
                {
                    Direction[] directions = { Direction.S, Direction.W, Direction.E, Direction.N };
                    player.Move(Direction.S, cancellationToken).Wait();
                    for (int i = 1; i < directions.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        await player.Move(directions[i], cancellationToken);
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await player.AcceptMessage(cancellationToken);
                    }
                }
            }
        }
    }
}
