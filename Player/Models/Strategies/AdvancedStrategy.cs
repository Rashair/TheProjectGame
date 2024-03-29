﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Player.Models.Strategies.AdvancedStrategyUtils;
using Player.Models.Strategies.Utils;
using Serilog;
using Shared.Enums;
using Shared.Models;

namespace Player.Models.Strategies;

public class AdvancedStrategy : IStrategy
{
    private const int NumberOfPossibleDirections = 4;
    private const int ALotOfFalseMoves = 8;
    private const int MaxDirectionsHistoryCount = 100;
    private const int DiscoverPenaltyThreshold = 10;

    private readonly RandomGenerator random;
    private readonly ILogger logger;
    private readonly Player player;

    // Strategy computed values
    private readonly LinkedList<Direction> directionsHistory;
    private (int y, int x) previousPosition;
    private int previousDistToPiece;
    private DiscoverState state;
    private LastAction lastAction;
    private ColumnGenerator columnGenerator;
    private (int numOnBoard, int checkedFieldsNum, bool[] checkedFields, int tier) column;

    public AdvancedStrategy(Player player, ILogger log)
    {
        this.logger = log.ForContext<AdvancedStrategy>();
        this.player = player;

        this.random = new RandomGenerator();
        this.previousPosition = (-1, -1);
        this.previousDistToPiece = int.MaxValue;
        this.directionsHistory = new LinkedList<Direction>();

        this.state = DiscoverState.ShouldDiscover;
        this.lastAction = LastAction.None;
    }

    private Direction PreviousDir => directionsHistory.Last.Value;

    // Contstant
    private Field[,] board;
    private (int y, int x) boardSize;
    private int goalAreaSize;
    private (int y1, int y2) goalAreaRange;
    private Penalties penalties;

    // Estimated values
    private double taskAreaToGoalField;
    private double taskAreaToPiece;
    private double goalFieldToPiece;
    private double taskAreaToGoalFieldCost;
    private double taskAreaToPieceCost;
    private double goalFieldToPieceCost;

    // Changing
    private CancellationToken cancellationToken;
    private int y;
    private int x;
    private int distToPiece;
    private List<int> askIds = new List<int>();
    private bool isInGoalArea;
    private bool isNotStuckOnPreviousPosition;
    private bool isReallyStuck;
    private bool estimationsInitialized;

    private void CacheCurrentState(CancellationToken token)
    {
        if (board == null)
        {
            board = player.Board;
            boardSize = player.BoardSize;
            goalAreaSize = player.GoalAreaSize;
            goalAreaRange = player.GoalAreaRange;
            penalties = player.PenaltiesTimes;
            directionsHistory.AddLast(player.GoalAreaDirection.GetOppositeDirection());

            columnGenerator = new ColumnGenerator(player.NormalizedID, boardSize.x, player.EnemiesIds.Length);
            logger.Information("Strategy info: " +
                $"{player.NormalizedID}| {boardSize.x}| {player.EnemiesIds.Length}");
            column.tier = 1;
            column.checkedFields = new bool[goalAreaSize];
            column.numOnBoard = columnGenerator.GetColumnToHandle(column.tier);
            logger.Information($"Got column: {column.numOnBoard}");
        }

        cancellationToken = token;
        (y, x) = player.Position;
        distToPiece = board[y, x].DistToPiece;

        isInGoalArea = y >= goalAreaRange.y1 && y < goalAreaRange.y2;

        isNotStuckOnPreviousPosition = lastAction != LastAction.Move ||
            (previousPosition.y != y || previousPosition.x != x);

        isReallyStuck = !isNotStuckOnPreviousPosition && player.NotMadeMoveInRow > ALotOfFalseMoves;

        if (lastAction == LastAction.Put && board[y, x].GoalInfo != GoalInfo.IDK)
        {
            ++column.checkedFieldsNum;
            column.checkedFields[y - goalAreaRange.y1] = true;
        }

        if (column.checkedFieldsNum == goalAreaSize)
        {
            GetNewColumn();
        }

        if (!estimationsInitialized)
        {
            EstimateDistances();
            GetEstimatedCosts();

            estimationsInitialized = true;
        }
    }

    private void GetEstimatedCosts()
    {
        taskAreaToGoalFieldCost = (taskAreaToGoalField * player.PenaltiesTimes.Move) + player.PenaltiesTimes.PutPiece;
        taskAreaToPieceCost = (taskAreaToPiece * player.PenaltiesTimes.Move) + player.PenaltiesTimes.Pickup;
        goalFieldToPieceCost = (goalFieldToPiece * player.PenaltiesTimes.Move) + player.PenaltiesTimes.Pickup;
    }

    private void EstimateDistances()
    {
        (int xFrom, int xTo, int yFrom, int yTo) taskAreaSize = (
            0, 
            player.BoardSize.x - 1, 
            player.GoalAreaSize, 
            player.BoardSize.y - player.GoalAreaSize - 1);

        (int xFrom, int xTo, int yFrom, int yTo) goalAreaSize = (
            0,
            player.BoardSize.x - 1, 
            0,
            player.GoalAreaSize - 1);

        TaskAreaToGoalFieldDistance(taskAreaSize, goalAreaSize);
        TaskAreaToPieceDistance(taskAreaSize);
        GoalFieldToPieceDistance(taskAreaSize, goalAreaSize);
    }

    private void TaskAreaToGoalFieldDistance((int xFrom, int xTo, int yFrom, int yTo) taskAreaSize, (int xFrom, int xTo, int yFrom, int yTo) goalAreaSize)
    {
        double sum = 0;
        for (int x1 = taskAreaSize.xFrom; x1 <= taskAreaSize.xTo; x1++)
        {
            for (int y1 = taskAreaSize.yFrom; y1 <= taskAreaSize.yTo; y1++)
            {
                (int x, int y) taskAreaPoint = (x1, y1);

                for (int x2 = goalAreaSize.xFrom; x2 <= goalAreaSize.xTo; x2++)
                {
                    for (int y2 = goalAreaSize.yFrom; y2 <= goalAreaSize.yTo; y2++)
                    {
                        (int x, int y) goalAreaPoint = (x2, y2);

                        sum += PlayerDistance(taskAreaPoint, goalAreaPoint);
                    }
                }
            }
        }

        int taskAreaFields = (taskAreaSize.yTo - taskAreaSize.yFrom + 1) * (taskAreaSize.xTo - taskAreaSize.xFrom + 1);
        int goalAreaFields = (goalAreaSize.yTo - goalAreaSize.yFrom + 1) * (goalAreaSize.xTo - goalAreaSize.xFrom + 1);

        taskAreaToGoalField = sum / (taskAreaFields * goalAreaFields);
    }

    private void TaskAreaToPieceDistance((int xFrom, int xTo, int yFrom, int yTo) taskAreaSize)
    {
        taskAreaToPiece = PlayerDistance((taskAreaSize.xFrom, taskAreaSize.yFrom), (taskAreaSize.xTo, taskAreaSize.yTo)) / 2.0;
    }

    private void GoalFieldToPieceDistance((int xFrom, int xTo, int yFrom, int yTo) taskAreaSize, (int xFrom, int xTo, int yFrom, int yTo) goalAreaSize)
    {
        goalFieldToPiece = PlayerDistance((goalAreaSize.xFrom, goalAreaSize.yFrom), (taskAreaSize.xTo, taskAreaSize.yTo)) / 2.0;
    }

    private void GetNewColumn()
    {
        InitColumnCheckedFields();
        ++column.tier;
        column.numOnBoard = columnGenerator.GetColumnToHandle(column.tier);
        logger.Information($"Got column: {column.numOnBoard}");
    }

    private void InitColumnCheckedFields()
    {
        column.checkedFieldsNum = 0;
        for (int i = 0; i < column.checkedFields.Length; ++i)
        {
            column.checkedFields[i] = false;
        }
    }

    public Task MakeDecision(CancellationToken cancellationToken)
    {
        CacheCurrentState(cancellationToken);

        Task decision;
        if (player.IsCommunicatinonWorthy)
        {
            if (player.IsCommunicationMaster)
            {
                if (player.GoalAreaChangesCount == player.MaxGoalAreaChanges)
                {
                    if (player.WaitingPlayers.Count == 0)
                    {
                        player.GoalAreaChangesCount = 0;
                    }
                    else
                    {
                        return player.GiveInfo(cancellationToken);
                    }
                }
                else if (player.IdToAsk != -1)
                {
                    var id = player.IdToAsk;
                    player.IdToAsk = -1;
                    return player.BegForInfo(cancellationToken, id);
                }
            }
            else if (player.GoalAreaState == GoalAreaActionsEnum.GoalAreaChanged && player.WaitingPlayers.Count > 0)
            {
                player.GoalAreaState = GoalAreaActionsEnum.None;
                return player.GiveInfo(cancellationToken);
            }
            else if (player.GoalAreaState == GoalAreaActionsEnum.ExchangedInfo)
            {
                player.GoalAreaState = GoalAreaActionsEnum.None;
                return player.BegForInfo(cancellationToken, player.CommunicationMasterId);
            }
        }

        if (isReallyStuck)
        {
            decision = IsReallyStuck();
        }
        else
        {
            decision = IsNotReallyStuck();
        }

        previousPosition = player.Position;
        previousDistToPiece = distToPiece;

        return decision;
    }

    private Task IsReallyStuck()
    {
        var directions = GetDirectionsInRange(0, boardSize.y - 1);
        var prevDirNode = directionsHistory.Last;
        if (prevDirNode != null && prevDirNode.Previous != null)
        {
            directions.Remove(prevDirNode.Value);
            directions.Remove(prevDirNode.Previous.Value);
        }

        return MoveToDirection(GetRandomDirection(directions));
    }

    private Task IsNotReallyStuck()
    {
        if (player.HasPiece)
        {
            return HasPiece();
        }
        else
        {
            return NoPiece();
        }
    }

    public Task NoPiece()
    {
        Task decision;
        if (isInGoalArea)
        {
            decision = NoPieceInGoalArea();
            state = DiscoverState.ShouldDiscover;
        }
        else
        {
            decision = NoPieceInTaskArea();
        }

        return decision;
    }

    private Task NoPieceInGoalArea()
    {
        Direction currentDirection;
        if (isNotStuckOnPreviousPosition)
        {
            currentDirection = player.GoalAreaDirection.GetOppositeDirection();
        }
        else
        {
            var directions = GetHorizontalDirections();
            currentDirection = GetRandomDirection(directions);
        }

        return MoveToDirection(currentDirection);
    }

    private Task NoPieceInTaskArea()
    {
        Task decision;
        if (distToPiece == 0)
        {
            decision = player.Pick(cancellationToken);
            state = DiscoverState.NoAction;
            lastAction = LastAction.Pick;
            logger.Information("Picked piece");
        }
        else if (ShouldDiscover())
        {
            decision = player.Discover(cancellationToken);
            state = DiscoverState.Discovered;
            lastAction = LastAction.Discover;
        }
        else
        {
            decision = NoPieceInTaskAreaMove();
        }

        return decision;
    }

    private bool ShouldDiscover()
    {
        if (penalties.Discovery > DiscoverPenaltyThreshold * penalties.Move)
        {
            return false;
        }

        return state == DiscoverState.ShouldDiscover ||
              (state != DiscoverState.Discovered && PreviousDir == Direction.FromCurrent);
    }

    private Task NoPieceInTaskAreaMove()
    {
        if (state == DiscoverState.Discovered)
        {
            return NoPieceInTaskAreaDiscovered();
        }
        else
        {
            return NoPieceInTaskAreaNotDiscovered();
        }
    }

    private Task NoPieceInTaskAreaDiscovered()
    {
        var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
        var moveDir = directions.Aggregate((d1, d2) =>
        {
            if (!isNotStuckOnPreviousPosition)
            {
                if (PreviousDir == d1)
                {
                    return d2;
                }
                if (PreviousDir == d2)
                {
                    return d1;
                }
            }
            (int y1, int x1) = d1.GetCoordinates((y, x));
            (int y2, int x2) = d2.GetCoordinates((y, x));

            return board[y1, x1].DistToPiece < board[y2, x2].DistToPiece ? d1 : d2;
        });

        state = DiscoverState.NoAction;
        return MoveToDirection(moveDir);
    }

    private Task NoPieceInTaskAreaNotDiscovered()
    {
        Direction currentDirection;
        var directions = GetDirectionsInRange(goalAreaSize, boardSize.y - goalAreaSize);
        bool isPreviousDirPossible = directions.Contains(PreviousDir);
        if (distToPiece < previousDistToPiece && isPreviousDirPossible && isNotStuckOnPreviousPosition)
        {
            currentDirection = PreviousDir;
        }
        else if (distToPiece > previousDistToPiece)
        {
            // Parallel piece
            if (WasWalkingBackAndForward())
            {
                state = DiscoverState.ShouldDiscover;
                currentDirection = GetRandomPerpendicularDirection(directions, PreviousDir);
            }
            else
            {
                currentDirection = PreviousDir.GetOppositeDirection();
            }
        }
        else
        {
            currentDirection = GetRandomPerpendicularDirection(directions, PreviousDir);
        }

        return MoveToDirection(currentDirection);
    }

    private bool WasWalkingBackAndForward()
    {
        if (directionsHistory.Count > 2)
        {
            var penultimateDir = directionsHistory.Last.Previous.Value;
            if (penultimateDir == PreviousDir)
            {
                var penultimatePos = penultimateDir.GetOppositeDirection().GetCoordinates(previousPosition);
                return board[y, x].DistToPiece != int.MaxValue &&
                    board[penultimatePos.y, penultimatePos.x].DistToPiece == board[y, x].DistToPiece;
            }

            return false;
        }

        return false;
    }

    private Task HasPiece()
    {
        bool shouldCheck = player.PenaltiesTimes.CheckForSham <= player.ShamPieceProbability * (goalFieldToPieceCost + taskAreaToGoalFieldCost - taskAreaToPieceCost);

        if (player.IsHeldPieceSham == null && shouldCheck)
        {
            lastAction = LastAction.Check;
            return player.CheckPiece(cancellationToken);
        }
        else if (player.IsHeldPieceSham == true)
        {
            lastAction = LastAction.Destroy;
            return player.DestroyPiece(cancellationToken);
        }
        else if (isInGoalArea)
        {
            return HasPieceInGoalArea();
        }
        else
        {
            return HasPieceInTaskArea();
        }
    }

    private Task HasPieceInGoalArea()
    {
        if (column.numOnBoard == x)
        {
            return HasPieceInGoalAreaOnCorrectColumn();
        }
        else
        {
            return HasPieceInGoalAreaOnIncorrectColumn();
        }
    }

    private Task HasPieceInGoalAreaOnCorrectColumn()
    {
        if (board[y, x].GoalInfo == GoalInfo.IDK)
        {
            lastAction = LastAction.Put;
            return player.Put(cancellationToken);
        }

        int ind = y - goalAreaRange.y1;
        if (!column.checkedFields[ind])
        {
            ++column.checkedFieldsNum;
            column.checkedFields[ind] = true;
            if (column.checkedFieldsNum == goalAreaSize)
            {
                GetNewColumn();
                return HasPieceInGoalAreaOnIncorrectColumn();
            }
        }

        var directions = GetVerticalDirections(goalAreaRange.y1, goalAreaRange.y2);
        var prevDir = PreviousDir;
        Direction currentDir;
        if (prevDir == Direction.E || prevDir == Direction.W)
        {
            currentDir = GetRandomDirection(directions);
        }
        else if (!isNotStuckOnPreviousPosition)
        {
            currentDir = prevDir.GetOppositeDirection();
        }
        else
        {
            currentDir = directions.Count > 1 ? prevDir :
                directions[0];
        }

        return MoveToDirection(currentDir);
    }

    private Task HasPieceInGoalAreaOnIncorrectColumn()
    {
        Direction currentDir;
        if (!isNotStuckOnPreviousPosition)
        {
            var directions = GetDirectionsInRange(goalAreaRange.y1, Math.Max(goalAreaRange.y2, 2));
            currentDir = GetRandomDirection(directions);
        }
        else
        {
            currentDir = GetCurrentColumnDirection();
        }

        return MoveToDirection(currentDir);
    }

    private Direction GetCurrentColumnDirection()
    {
        return x < column.numOnBoard ? Direction.E : Direction.W;
    }

    private Task HasPieceInTaskArea()
    {
        Direction moveDirection;
        var prevDir = PreviousDir;
        if (isNotStuckOnPreviousPosition || prevDir != player.GoalAreaDirection)
        {
            if (prevDir == player.GoalAreaDirection && x != column.numOnBoard)
            {
                moveDirection = GetCurrentColumnDirection();
            }
            else
            {
                moveDirection = player.GoalAreaDirection;
            }
        }
        else
        {
            moveDirection = GetDirectionWhenStuck(horizontal: true);
        }

        return MoveToDirection(moveDirection);
    }

    //// Utilites
    //// -------------------------------------------------------------------------------------------
    
    private int PlayerDistance((int x, int y) p1, (int x, int y) p2)
    {
        return Math.Max(Math.Abs(p2.x - p1.x), Math.Abs(p2.y - p1.y));
    }

    private List<Direction> GetDirectionsInRange(int y1, int y2)
    {
        List<Direction> directions = new List<Direction>(NumberOfPossibleDirections);
        if (x > 0)
            directions.Add(Direction.W);
        if (x < boardSize.x - 1)
            directions.Add(Direction.E);

        if (y > y1)
            directions.Add(Direction.S);
        if (y < y2 - 1)
            directions.Add(Direction.N);

        return directions;
    }

    private List<Direction> GetHorizontalDirections()
    {
        List<Direction> directions = new List<Direction>(NumberOfPossibleDirections / 2);
        if (x > 0)
            directions.Add(Direction.W);
        if (x < boardSize.x - 1)
            directions.Add(Direction.E);

        return directions;
    }

    private List<Direction> GetVerticalDirections(int y1, int y2)
    {
        List<Direction> directions = new List<Direction>(NumberOfPossibleDirections / 2);
        if (y > y1)
            directions.Add(Direction.S);
        if (y < y2 - 1)
            directions.Add(Direction.N);

        return directions;
    }

    private Direction GetRandomDirection(List<Direction> directions)
    {
        return directions[random[directions.Count]];
    }

    private Direction GetRandomPerpendicularDirection(List<Direction> directions, Direction previousDir)
    {
        var (right, left) = previousDir.GetPerpendicularDirections();
        Direction result;
        try
        {
            result = GetRandomDirection(directions, right, left);
        }
        catch (InvalidOperationException)
        {
            logger.Warning("Error in strategy logic. This case should never happen.");
            result = GetRandomDirection(directions);
        }

        return result;
    }

    private Direction GetRandomDirection(List<Direction> directions, Direction right, Direction left)
    {
        int startInd = random[directions.Count];
        int ind = startInd;
        do
        {
            if (directions[ind] == right)
            {
                return right;
            }
            else if (directions[ind] == left)
            {
                return left;
            }

            ++ind;
            if (ind == directions.Count)
            {
                ind = 0;
            }
        }
        while (ind != startInd);

        throw new InvalidOperationException($"None of the provided directions were in list, " +
            $"length: {directions.Count}, left: {left}, right: {right}");
    }

    private Task MoveToDirection(Direction dir)
    {
        lastAction = LastAction.Move;
        directionsHistory.AddLast(dir);
        if (directionsHistory.Count > MaxDirectionsHistoryCount)
        {
            directionsHistory.RemoveFirst();
        }

        return player.Move(dir, cancellationToken);
    }

    private Direction GetDirectionWhenStuck(bool horizontal)
    {
        List<Direction> directions;
        if (horizontal)
        {
            directions = GetHorizontalDirections();
        }
        else
        {
            directions = GetVerticalDirections(0, boardSize.y);
        }

        var prevDir = PreviousDir;
        if (directions.Count > 1)
        {
            if (prevDir == directions[0])
            {
                return directions[1];
            }
            else if (prevDir == directions[1])
            {
                return directions[0];
            }
            else
            {
                return GetRandomDirection(directions);
            }
        }

        return directions[0];
    }
}
