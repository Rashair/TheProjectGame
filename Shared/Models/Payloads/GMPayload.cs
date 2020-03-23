using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Models.Payloads
{
    public class CheckAnswerPayload
    {
        public bool sham;
    }

    public class EmptyAnswerPayload
    {
    }

    public class DiscoveryAnswerPayload
    {
        public int distanceFromCurrent;
        public int distanceN;
        public int distanceNE;
        public int distanceE;
        public int distanceSE;
        public int distanceS;
        public int distanceSW;
        public int distanceW;
        public int distanceNW;
    }

    public class EndGamePayload
    {
        public string winner;
    }

    public class BoardSize
    {
        public int x;
        public int y;
    }

    public class NumberOfPlayers
    {
        public int allies;
        public int enemies;
    }

    public class Penalties
    {
        public string move;
        public string checkForSham;
        public string discovery;
        public string destroyPiece;
        public string putPiece;
        public string informationExchange;
    }

    public class Position
    {
        public int x;
        public int y;
    }

    public class StartGamePayload
    {
        public int agentID;
        public int[] alliesIDs;
        public int leaderID;
        public int[] enemiesIDs;
        public string teamId;
        public BoardSize boardSize;
        public int goalAreaSize;
        public NumberOfPlayers numberOfPlayers;
        public int numberOfPieces;
        public int numberOfGoals;
        public Penalties penalties;
        public float shamPieceProbability;
        public Position position;
    }

    public class BegForInfoForwardedPayload
    {
        public int askingID;
        public bool leader;
        public string teamId;
    }

    public class JoinAnswerPayload
    {
        public bool accepted;
        public int agentID;
    }
    public class MoveAnswerPayload
    {
        public bool madeMove;
        public Position currentPosition;
        public int closestPiece;
    }

    public class GiveInfoForwardedPayload
    {
        public int answeringID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    } //added for compatibility
}