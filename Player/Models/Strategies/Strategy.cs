using System;
using System.Collections.Generic;
using System.Threading;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class Strategy : IStrategy
    {
        public void MakeDecision(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            if (!player.HavePiece)
            {
                player.Discover(cancellationToken).Wait();
                if (!cancellationToken.IsCancellationRequested)
                {
                    player.AcceptMessage(cancellationToken).Wait();

                    (int x, int y) = player.Position;
                    (Direction, int, int)[] neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter((x, y));
                    int[] dist = new int[neighbourCoordinates.Length];

                    for (int i = 0; i < neighbourCoordinates.Length; i++)
                    {
                        dist[i] = player.Board[neighbourCoordinates[i].Item2, neighbourCoordinates[i].Item3].DistToPiece;
                    }
                    Array.Sort(dist, neighbourCoordinates);

                    player.Move(neighbourCoordinates[0].Item1, cancellationToken).Wait();
                    for (int i = 1; i < dist.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        player.Move(neighbourCoordinates[i].Item1, cancellationToken).Wait();
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
                                player.Move(Direction.N, cancellationToken).Wait();
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    player.AcceptMessage(cancellationToken).Wait();
                                }
                            }
                            else
                            {
                                RedPlayerMoveToGoal(player, team, goalAreaSize, cancellationToken);
                            }
                            break;
                        }
                    case Team.Blue:
                        {
                            if (player.Position.Item2 >= goalAreaSize)
                            {
                                player.Move(Direction.S, cancellationToken).Wait();
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    player.AcceptMessage(cancellationToken).Wait();
                                }
                            }
                            else
                            {
                                BluePlayerMoveToGoal(player, team, goalAreaSize, cancellationToken);
                            }
                            break;
                        }
                }
            }
        }

        public void RedPlayerMoveToGoal(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            while (player.Position.Item2 < player.BoardSize.y)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    player.Put(cancellationToken).Wait();
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(cancellationToken).Wait();
                    }
                    break;
                }
                else
                {
                    Direction[] directions = { Direction.N, Direction.W, Direction.E, Direction.S };
                    player.Move(Direction.S, cancellationToken).Wait();
                    for (int i = 1; i < directions.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        player.Move(directions[i], cancellationToken).Wait();
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(cancellationToken).Wait();
                    }
                }
            }
        }

        public void BluePlayerMoveToGoal(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            while (player.Position.Item2 >= 0)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    player.Put(cancellationToken).Wait();
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(cancellationToken).Wait();
                    }
                    break;
                }
                else
                {
                    Direction[] directions = { Direction.S, Direction.W, Direction.E, Direction.N };
                    player.Move(Direction.S, cancellationToken).Wait();
                    for (int i = 1; i < directions.Length && cancellationToken.IsCancellationRequested; i++)
                    {
                        player.Move(directions[i], cancellationToken).Wait();
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(cancellationToken).Wait();
                    }
                }
            }
        }
    }
}
