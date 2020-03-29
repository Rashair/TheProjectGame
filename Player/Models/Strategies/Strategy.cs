﻿using System;
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
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await player.AcceptMessage(cancellationToken);
                            }
                        }
                        else
                        {
                            RedPlayerMoveToGoalAsync(player, cancellationToken);
                        }
                        break;
                    }
                    case Team.Blue:
                    {
                        if (player.Position.Item2 >= player.GoalAreaSize)
                        {
                            await player.Move(Direction.S, cancellationToken);
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await player.AcceptMessage(cancellationToken);
                            }
                        }
                        else
                        {
                            BluePlayerMoveToGoalAsync(player, cancellationToken);
                        }
                        break;
                    }
                }
            }
        }

        public async System.Threading.Tasks.Task RedPlayerMoveToGoalAsync(Player player, CancellationToken cancellationToken)
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

        public async System.Threading.Tasks.Task BluePlayerMoveToGoalAsync(Player player, CancellationToken cancellationToken)
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
