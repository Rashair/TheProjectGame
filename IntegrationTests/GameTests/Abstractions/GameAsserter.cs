using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using GameMaster.Models.Fields;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests.GameTests.Abstractions
{
    public class GameAsserter
    {
        private readonly GameTestConfiguration testConf;
        private readonly GM gameMaster;
        private readonly List<Player.Models.Player> teamRed;
        private readonly List<Player.Models.Player> teamBlue;

        public GameAsserter(GameTestConfiguration testConf,
            List<Player.Models.Player> teamRed, List<Player.Models.Player> teamBlue, GM gameMaster)
        {
            this.testConf = testConf;
            this.gameMaster = gameMaster;
            this.teamRed = teamRed;
            this.teamBlue = teamBlue;
        }

        public async Task CheckStart()
        {
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");

            var conf = gameMaster.GetValue<GM, GameConfiguration>("conf");
            var (success, errorMessage) = await Shared.Helpers.Retry(() =>
            {
                return Task.FromResult(gameMaster.WasGameStarted);
            }, conf.NumberOfPlayersPerTeam, 3000, CancellationToken.None);
            Assert.Equal(conf.NumberOfPlayersPerTeam, gameMaster.Invoke<GM, int>("GetPlayersCount", Team.Red));
            Assert.Equal(conf.NumberOfPlayersPerTeam, gameMaster.Invoke<GM, int>("GetPlayersCount", Team.Blue));
            Assert.True(success, "Game should be started");

            Assert.True(teamRed.Any(p => p.IsLeader), "Team red should have leader");
            var playerRed = teamRed[0];
            Assert.True(playerRed.Team == Team.Red, "Player should have team passed with conf");
            Assert.True(playerRed.Position.y >= 0, "Player should have position set.");
            Assert.True(playerRed.Position.y < conf.Height - conf.GoalAreaHeight, "Player should not be present on enemy team field");

            Assert.True(teamBlue.Any(p => p.IsLeader), "Team blue should have leader");
            var playerBlue = teamBlue[0];
            Assert.True(playerBlue.Team == Team.Blue, "Player should have team passed with conf");
            Assert.True(playerBlue.Position.y >= 0, "Player should have position set.");
            Assert.True(playerBlue.Position.y >= conf.GoalAreaHeight, "Player should not be present on enemy team field");
        }

        public async Task CheckRuntime()
        {
            var teamRedPositions = teamRed.Select(player => player.Position).ToList();
            var positionsCounterRed = new int[teamRedPositions.Count];

            var teamBluePositions = teamBlue.Select(player => player.Position).ToList();
            var positionsCounterBlue = new int[teamBluePositions.Count];

            var oneRowBoard = gameMaster.GetValue<GM, AbstractField[][]>("board").SelectMany(row => row);
            var piecesPositions = oneRowBoard.Where(field => field.ContainsPieces()).ToList();

            while (!gameMaster.WasGameFinished)
            {
                await Task.Delay(testConf.CheckInterval);

                AssertNewPiecesAreGenerated(oneRowBoard, ref piecesPositions);
                AssertPositionsChange(teamRed, teamRedPositions, positionsCounterRed);
                AssertPositionsChange(teamBlue, teamBluePositions, positionsCounterBlue);
            }
        }

        private void AssertNewPiecesAreGenerated(IEnumerable<AbstractField> board, ref List<AbstractField> oldPiecesPositions)
        {
            var newPiecesPositions = board.Where(field => field.ContainsPieces()).ToList();

            bool anyNewPieces = oldPiecesPositions.Any(pos =>
                newPiecesPositions.Any(newPos => !newPos.Equals(pos) || newPos.PiecesCount != pos.PiecesCount));
            Assert.True(anyNewPieces, "GM should generate some new pieces");

            oldPiecesPositions = newPiecesPositions;
        }

        private void AssertPositionsChange(List<Player.Models.Player> team, List<(int y, int x)> teamPositions, int[] positionsCounter)
        {
            for (int i = 0; i < team.Count; ++i)
            {
                if (team[i].Position == teamPositions[i])
                {
                    ++positionsCounter[i];
                    Assert.False(positionsCounter[i] > testConf.PositionNotChangedThreshold, "Player should not be stuck on one position");
                }
                else
                {
                    teamPositions[i] = team[i].Position;
                    positionsCounter[i] = 0;
                }
            }
        }

        public void CheckEnd()
        {
            var winnerRed = teamRed[0].GetValue<Player.Models.Player, Team?>("winner");
            Assert.False(winnerRed == null, "Winner should not be null");
            var winnerBlue = teamBlue[0].GetValue<Player.Models.Player, Team?>("winner");
            Assert.True(winnerRed == winnerBlue,
                "Players should have same winner saved");

            var redPoints = gameMaster.GetValue<GM, int>("redTeamPoints");
            var bluePoints = gameMaster.GetValue<GM, int>("blueTeamPoints");
            var expectedWinner = redPoints > bluePoints ? Team.Red : Team.Blue;
            Assert.True(winnerRed == expectedWinner, "GM and players should have same winner");
        }
    }
}
