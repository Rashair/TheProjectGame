using System;
using System.Collections.Generic;
using System.Threading;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class Strategy : IStrategy
    {
        public void MakeDecision(Player player, Team team, int goalAreaSize)
        {
            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            if (!player.HavePiece)
            {
                CancellationToken token = cancelTokenSource.Token;
                player.Discover(token).Wait();
                if (!token.IsCancellationRequested)
                {
                    player.AcceptMessage(token).Wait();

                    (int x, int y) = player.Position;
                    (Direction, int, int)[] neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter((x, y));
                    int[] dist = new int[neighbourCoordinates.Length];

                    for (int i = 0; i < neighbourCoordinates.Length; i++)
                    {
                        dist[i] = player.Board[neighbourCoordinates[i].Item2, neighbourCoordinates[i].Item3].DistToPiece;
                    }
                    Array.Sort(dist, neighbourCoordinates);

                    CancellationToken moveToken = cancelTokenSource.Token;
                    player.Move(neighbourCoordinates[0].Item1, moveToken).Wait();
                    for (int i = 1; i < dist.Length && moveToken.IsCancellationRequested; i++)
                    {
                        moveToken = cancelTokenSource.Token;
                        player.Move(neighbourCoordinates[i].Item1, moveToken).Wait();
                    }
                    if (!moveToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(moveToken).Wait();
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
                                CancellationToken moveToken = cancelTokenSource.Token;
                                player.Move(Direction.N, moveToken).Wait();
                                if (!moveToken.IsCancellationRequested)
                                {
                                    player.AcceptMessage(moveToken).Wait();
                                }
                            }
                            else
                            {
                                RedPlayerMoveToGoal(player, team, goalAreaSize, cancelTokenSource);
                            }
                            break;
                        }
                    case Team.Blue:
                        {
                            if (player.Position.Item2 >= goalAreaSize)
                            {
                                CancellationToken moveToken = cancelTokenSource.Token;
                                player.Move(Direction.S, moveToken).Wait();
                                if (!moveToken.IsCancellationRequested)
                                {
                                    player.AcceptMessage(moveToken).Wait();
                                }
                            }
                            else
                            {
                                BluePlayerMoveToGoal(player, team, goalAreaSize, cancelTokenSource);
                            }
                            break;
                        }
                }
            }
        }

        public void RedPlayerMoveToGoal(Player player, Team team, int goalAreaSize, CancellationTokenSource cancelTokenSource)
        {
            while (player.Position.Item2 < player.BoardSize.y)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    CancellationToken putToken = cancelTokenSource.Token;
                    player.Put(putToken).Wait();
                    if (!putToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(putToken).Wait();
                    }
                    break;
                }
                else
                {
                    CancellationToken moveToken = cancelTokenSource.Token;
                    Direction[] directions = { Direction.N, Direction.W, Direction.E, Direction.S };
                    player.Move(Direction.S, moveToken).Wait();
                    for (int i = 1; i < directions.Length && moveToken.IsCancellationRequested; i++)
                    {
                        moveToken = cancelTokenSource.Token;
                        player.Move(directions[i], moveToken).Wait();
                    }
                    if (!moveToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(moveToken).Wait();
                    }
                }
            }
        }

        public void BluePlayerMoveToGoal(Player player, Team team, int goalAreaSize, CancellationTokenSource cancelTokenSource)
        {
            while (player.Position.Item2 >= 0)
            {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    CancellationToken putToken = cancelTokenSource.Token;
                    player.Put(putToken).Wait();
                    if (!putToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(putToken).Wait();
                    }
                    break;
                }
                else
                {
                    CancellationToken moveToken = cancelTokenSource.Token;
                    Direction[] directions = { Direction.S, Direction.W, Direction.E, Direction.N };
                    player.Move(Direction.S, moveToken).Wait();
                    for (int i = 1; i < directions.Length && moveToken.IsCancellationRequested; i++)
                    {
                        moveToken = cancelTokenSource.Token;
                        player.Move(directions[i], moveToken).Wait();
                    }
                    if (!moveToken.IsCancellationRequested)
                    {
                        player.AcceptMessage(moveToken).Wait();
                    }
                }
            }
        }
    }
}
