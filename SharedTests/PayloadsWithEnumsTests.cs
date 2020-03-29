﻿using System.Collections.Generic;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads;
using Xunit;

namespace Shared.Tests
{
    public class PayloadsWithEnumsTests
    {
        [Fact]
        public void TestBegForInfoForwardedPayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"AskingID\":0,\"Leader\":true,\"teamId\":\"Blue\"}";

            // Act
            Team expectedTeam = Team.Blue;
            var deserializedObject = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(jsonString);

            // Assert
            Assert.Equal(expectedTeam, deserializedObject.TeamId);
        }

        [Fact]
        public void TestBegForInfoForwardedPayloadSerialization()
        {
            // Arrange
            var payload = new BegForInfoForwardedPayload()
            {
                AskingID = 0,
                Leader = true,
                TeamId = Team.Red,
            };

            // Act
            var expectedJsonString = "{\"AskingID\":0,\"Leader\":true,\"teamId\":\"Red\"}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }

        [Fact]
        public void TestEndGamePayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"winner\":\"Red\"}";

            // Act
            Team expectedTeam = Team.Red;
            var deserializedObject = JsonConvert.DeserializeObject<EndGamePayload>(jsonString);

            // Assert
            Assert.Equal(expectedTeam, deserializedObject.Winner);
        }

        [Fact]
        public void TestEndGamePayloadSerialization()
        {
            // Arrange
            var payload = new EndGamePayload()
            {
                Winner = Team.Red,
            };

            // Act
            var expectedJsonString = "{\"winner\":\"Red\"}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }

        [Fact]
        public void TestGiveInfoForwardedPayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"AnsweringID\":1,\"Distances\":[[1,2],[0,1]]," +
                "\"redTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]," +
                "\"blueTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]}";
            int x = 2, y = 2;
            GoalInfo[,] expectedRedTeamGoalAreaInformations = new GoalInfo[x, y];
            GoalInfo[,] expectedBlueTeamGoalAreaInformations = new GoalInfo[x, y];

            // Act
            expectedRedTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            expectedRedTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            expectedRedTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            expectedRedTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            expectedBlueTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            expectedBlueTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            expectedBlueTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            expectedBlueTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;
            var deserializedObject = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(jsonString);

            // Assert
            Assert.Equal(expectedRedTeamGoalAreaInformations, deserializedObject.RedTeamGoalAreaInformations);
            Assert.Equal(expectedBlueTeamGoalAreaInformations, deserializedObject.BlueTeamGoalAreaInformations);
        }

        [Fact]
        public void TestGiveInfoForwardedPayloadSerialization()
        {
            // Arrange
            var payload = new GiveInfoForwardedPayload()
            {
                AnsweringID = 1,
            };
            payload.Distances = new int[2, 2];
            payload.RedTeamGoalAreaInformations = new GoalInfo[2, 2];
            payload.BlueTeamGoalAreaInformations = new GoalInfo[2, 2];
            payload.Distances[0, 0] = 1;
            payload.Distances[0, 1] = 0;
            payload.Distances[0, 1] = 2;
            payload.Distances[1, 1] = 1;

            payload.RedTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            payload.RedTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            payload.RedTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            payload.RedTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            payload.BlueTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            payload.BlueTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            payload.BlueTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            payload.BlueTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            // Act
            var expectedJsonString = "{\"AnsweringID\":1,\"Distances\":[[1,2],[0,1]]," +
                "\"redTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]," +
                "\"blueTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }

        [Fact]
        public void TestGiveInfoPayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"Distances\":[[1,2],[0,1]],\"RespondToID\":1," +
                "\"redTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]," +
                "\"blueTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]}";
            int x = 2, y = 2;
            GoalInfo[,] expectedRedTeamGoalAreaInformations = new GoalInfo[x, y];
            GoalInfo[,] expectedBlueTeamGoalAreaInformations = new GoalInfo[x, y];

            // Act
            expectedRedTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            expectedRedTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            expectedRedTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            expectedRedTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            expectedBlueTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            expectedBlueTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            expectedBlueTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            expectedBlueTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;
            var deserializedObject = JsonConvert.DeserializeObject<GiveInfoPayload>(jsonString);

            // Assert
            Assert.Equal(expectedRedTeamGoalAreaInformations, deserializedObject.RedTeamGoalAreaInformations);
            Assert.Equal(expectedBlueTeamGoalAreaInformations, deserializedObject.BlueTeamGoalAreaInformations);
        }

        [Fact]
        public void TestGiveInfoPayloadSerialization()
        {
            // Arrange
            var payload = new GiveInfoPayload()
            {
                RespondToID = 1,
            };
            payload.Distances = new int[2, 2];
            payload.RedTeamGoalAreaInformations = new GoalInfo[2, 2];
            payload.BlueTeamGoalAreaInformations = new GoalInfo[2, 2];
            payload.Distances[0, 0] = 1;
            payload.Distances[0, 1] = 0;
            payload.Distances[0, 1] = 2;
            payload.Distances[1, 1] = 1;

            payload.RedTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            payload.RedTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            payload.RedTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            payload.RedTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            payload.BlueTeamGoalAreaInformations[0, 0] = GoalInfo.IDK;
            payload.BlueTeamGoalAreaInformations[0, 1] = GoalInfo.IDK;
            payload.BlueTeamGoalAreaInformations[1, 0] = GoalInfo.DiscoveredNotGoal;
            payload.BlueTeamGoalAreaInformations[1, 1] = GoalInfo.DiscoveredGoal;

            // Act
            var expectedJsonString = "{\"Distances\":[[1,2],[0,1]],\"RespondToID\":1," +
                "\"redTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]," +
                "\"blueTeamGoalAreaInformations\":[[\"IDK\",\"IDK\"],[\"DiscoveredNotGoal\",\"DiscoveredGoal\"]]}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }

        [Fact]
        public void TestPickErrorPayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"errorSubtype\":\"NothingThere\"}";

            // Act
            PickError expectedPickError = PickError.NothingThere;
            var deserializedObject = JsonConvert.DeserializeObject<PickErrorPayload>(jsonString);

            // Assert
            Assert.Equal(expectedPickError, deserializedObject.ErrorSubtype);
        }

        [Fact]
        public void TestPickErrorPayloadSerialization()
        {
            // Arrange
            var payload = new PickErrorPayload()
            {
                ErrorSubtype = PickError.NothingThere,
            };

            // Act
            var expectedJsonString = "{\"errorSubtype\":\"NothingThere\"}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }

        [Fact]
        public void TestPutErrorPayloadDeserialization()
        {
            // Arrange
            var jsonString = "{\"errorSubtype\":\"AgentNotHolding\"}";

            // Act
            PutError expectedPutError = PutError.AgentNotHolding;
            var deserializedObject = JsonConvert.DeserializeObject<PutErrorPayload>(jsonString);

            // Assert
            Assert.Equal(expectedPutError, deserializedObject.ErrorSubtype);
        }

        [Fact]
        public void TestPutErrorPayloadSerialization()
        {
            // Arrange
            var payload = new PutErrorPayload()
            {
                ErrorSubtype = PutError.CannotPutThere,
            };

            // Act
            var expectedJsonString = "{\"errorSubtype\":\"CannotPutThere\"}";
            var serializedPayload = payload.Serialize();

            // Assert
            Assert.Equal(expectedJsonString, serializedPayload);
        }
    }
}