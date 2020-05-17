using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Player.Models.Strategies.Utils;
using Shared.Enums;

namespace Player.Models.Strategies
{
    public class AdvancedStrategy : IStrategy
    {
        private const int NumberOfPossibleDirections = 4;
        private readonly Random random = new Random();
        private readonly Player player;

        private (int y, int x) previousPosition;
        private int previousDistToPiece;
        private Direction previousDirection;

        private DiscoverState state;

        public AdvancedStrategy(Player player)
        {
            this.player = player;

            this.random = new Random();
            this.previousPosition = (-1, -1);
            this.previousDistToPiece = int.MaxValue;
            this.previousDirection = Direction.FromCurrent;
            this.state = DiscoverState.ShouldDiscover;
        }

        public Field[,] Board => player.Board;

        public int CurrentDistToPiece => player.Board[player.Position.y, player.Position.x].DistToPiece;

        public Task MakeDecision(CancellationToken cancellationToken)
        {
            Task decision;
            if (!player.HasPiece)
            {
                decision = DoesNotHavePieceDecision(cancellationToken);
            }
            else
            {
                decision = HasPieceDecision(cancellationToken);
            }

            return decision;
        }

        public Task DoesNotHavePieceDecision(CancellationToken cancellationToken)
        {
            Task decision;
            (int y, int x) = player.Position;
            int distToPiece = player.Board[y, x].DistToPiece;
            if (IsInGoalArea())
            {
                state = DiscoverState.ShouldDiscover;
                decision = DoesNotHavePieceInGoalAreaMoveDecision(cancellationToken);
            }
            else if (distToPiece == 0)
            {
                decision = player.Pick(cancellationToken);
                state = DiscoverState.NoAction;
            }
            else if (state == DiscoverState.ShouldDiscover)
            {
                decision = player.Discover(cancellationToken);
                state = DiscoverState.Discovered;
            }
            else
            {
                decision = DoesNotHavePieceInTaskAreaMoveDecision(cancellationToken);
            }

            previousPosition = player.Position;
            previousDistToPiece = distToPiece;

            return decision;
        }

        private Task DoesNotHavePieceInGoalAreaMoveDecision(CancellationToken cancellationToken)
        {
            Direction currentDirection;
            if (previousDistToPiece == 0 || previousPosition != player.Position)
            {
                currentDirection = player.GoalAreaDirection.GetOppositeDirection();
            }
            else
            {
                var directions = GetDirectionsInRange(int.MaxValue, int.MinValue);
                currentDirection = GetRandomDirection(directions);
            }

            previousDirection = currentDirection;

            return player.Move(currentDirection, cancellationToken);
        }

        private Task DoesNotHavePieceInTaskAreaMoveDecision(CancellationToken cancellationToken)
        {
            Direction currentDirection;
            if (state == DiscoverState.Discovered)
            {
                int goalAreaSize = player.GoalAreaSize;
                var directions = GetDirectionsInRange(goalAreaSize, player.BoardSize.y - goalAreaSize);
                currentDirection = directions.Aggregate((d1, d2) =>
                {
                    (int y1, int x1) = d1.GetCoordinates(player.Position);
                    (int y2, int x2) = d2.GetCoordinates(player.Position);

                    return player.Board[y1, x1].DistToPiece < player.Board[y2, x2].DistToPiece ? d1 : d2;
                });

                state = DiscoverState.NoAction;
            }
            else
            {
                int goalAreaSize = player.GoalAreaSize;
                var directions = GetDirectionsInRange(goalAreaSize, player.BoardSize.y - goalAreaSize);

                int currDist = CurrentDistToPiece;
                bool isPreviousDirPossible = directions.Contains(previousDirection);
                if (currDist < previousDistToPiece && isPreviousDirPossible)
                {
                    currentDirection = previousDirection;
                }
                else if (!isPreviousDirPossible)
                {
                    var (right, left) = previousDirection.GetPerpendicularDirections();
                    if (directions.Contains(right))
                    {
                        currentDirection = right;
                    }
                    else
                    {
                        currentDirection = left;
                    }
                }
                else //// if (currDist >= previousDistToPiece)
                {
                    currentDirection = previousDirection.GetOppositeDirection();
                    if (currDist == previousDistToPiece)
                    {
                        state = DiscoverState.ShouldDiscover;
                    }
                }
            }

            previousDirection = currentDirection;

            return player.Move(currentDirection, cancellationToken);
        }

        private Task HasPieceDecision(CancellationToken cancellationToken)
        {
            if (player.IsHeldPieceSham == null)
            {
                return player.CheckPiece(cancellationToken);
            }
            else if (player.IsHeldPieceSham == true)
            {
                return player.DestroyPiece(cancellationToken);
            }
            else if (IsInGoalArea())
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

        private bool IsInGoalArea()
        {
            int y = player.Position.y;

            return y >= player.GoalAreaRange.y1 && y < player.GoalAreaRange.y2;
        }

        private Direction GetRandomDirection(List<Direction> directions)
        {
            return directions[random.Next(directions.Count)];
        }
    }
}
