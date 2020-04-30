using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GameMaster.Models;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests.GameTests.Abstractions
{
    internal class GameAsserter
    {
        private readonly GameTestConfiguration conf;
        private readonly GM gameMaster;
        private readonly List<Player.Models.Player> teamRed;
        private readonly List<Player.Models.Player> teamBlue;

        public GameAsserter(GameTestConfiguration conf, List<Player.Models.Player> teamRed, List<Player.Models.Player> teamBlue,
            GM gameMaster)
        {
            this.conf = conf;
            this.gameMaster = gameMaster;
            this.teamRed = teamRed;
            this.teamBlue = teamBlue;
        }

        public async Task CheckRuntime()
        {
            var teamRedPositions = teamRed.Select(player => player.Position).ToList();
            var positionsCounterRed = new int[teamRedPositions.Count];

            var teamBluePositions = teamBlue.Select(player => player.Position).ToList();
            var positionsCounterBlue = new int[teamBluePositions.Count];

            while (!gameMaster.WasGameFinished)
            {
                var timeNow = DateTime.Now;

                int waitTimeLeft = conf.PositionsCheckInterval - (int)(timeNow - DateTime.Now).TotalMilliseconds;
                await Task.Delay(waitTimeLeft);
                AssertPositionsChange(teamRed, teamRedPositions, positionsCounterRed);
                AssertPositionsChange(teamBlue, teamBluePositions, positionsCounterBlue);
            }
        }

        private void AssertPositionsChange(List<Player.Models.Player> team, List<(int y, int x)> teamPositions, int[] positionsCounter)
        {
            for (int i = 0; i < team.Count; ++i)
            {
                if (team[i].Position == teamPositions[i])
                {
                    ++positionsCounter[i];
                    Assert.False(positionsCounter[i] > conf.PositionNotChangedThreshold, "Player should not be stuck on one position");
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
