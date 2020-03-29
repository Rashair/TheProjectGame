using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class Strategy : IStrategy
    {
        public async Task MakeDecision(Player player, CancellationToken cancellationToken)
        {
            if (!player.HavePiece)
            {
                (int y, int x) = player.Position;
                if (player.Board[y, x].DistToPiece == 0)
                {
                    await player.Pick(cancellationToken);
                    return;
                }
            }
            else
            {
                switch (player.Team)
                {
                    case Team.Red:
                        {
                            if (player.Position.Item2 <= player.BoardSize.y - player.GoalAreaSize)
                            {
                                await player.Move(Direction.N, cancellationToken);
                            }
                            else
                            {
                                PlayerMoveToGoalAsync(player, player.Team, player.GoalAreaSize, cancellationToken);
                            }
                            break;
                        }
                    case Team.Blue:
                        {
                            if (player.Position.Item2 >= player.GoalAreaSize)
                            {
                                await player.Move(Direction.S, cancellationToken);
                            }
                            else
                            {
                                PlayerMoveToGoalAsync(player, player.Team, player.GoalAreaSize, cancellationToken);
                                PlayerMoveToGoalAsync(player, player.Team, player.GoalAreaSize, cancellationToken);
                            }
                            break;
                        }
                }
            }
        }

        public async System.Threading.Tasks.Task PlayerMoveToGoalAsync(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken)
        {
            GoalInfo info = player.Board[player.Position.Item1, player.Position.Item2].GoalInfo;
            if (info == GoalInfo.IDK)
            {
                await player.Put(cancellationToken);
            }
            else
            {
                Random rnd = new Random();
                int dir = rnd.Next(0, 4);
                if (dir == 1)
                {
                    await player.Move(Direction.S, cancellationToken);
                }
                if (dir == 1)
                {
                    await player.Move(Direction.N, cancellationToken);
                }
                if (dir == 1)
                {
                    await player.Move(Direction.E, cancellationToken);
                }
                if (dir == 1)
                {
                    await player.Move(Direction.W, cancellationToken);
                }
            }
        }
    }
}
