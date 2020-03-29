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
                }
                else
                {
                    await player.Move(Direction.S, cancellationToken);
                }
            }
        }

        public async System.Threading.Tasks.Task BluePlayerMoveToGoalAsync(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
                GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
                if (info == GoalInfo.IDK)
                {
                    await player.Put(cancellationToken);
                }
                else
                {
                    await player.Move(Direction.S, cancellationToken);
                }
            }
        }
    }
}
