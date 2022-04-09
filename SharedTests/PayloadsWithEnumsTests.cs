using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;
using Xunit;

namespace Shared.Tests;

public class PayloadsWithEnumsTests
{
    [Fact]
    public void TestBegForInfoForwardedPayloadDeserialization()
    {
        // Arrange
        var jsonString = "{\"askingID\":0,\"leader\":true,\"teamID\":\"blue\"}";

        // Act
        Team expectedTeam = Team.Blue;
        var deserializedObject = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(jsonString);

        // Assert
        Assert.Equal(expectedTeam, deserializedObject.TeamID);
    }

    [Fact]
    public void TestBegForInfoForwardedPayloadSerialization()
    {
        // Arrange
        var payload = new BegForInfoForwardedPayload()
        {
            AskingID = 0,
            Leader = true,
            TeamID = Team.Red,
        };

        // Act
        var expectedJsonString = "{\"askingID\":0,\"leader\":true,\"teamID\":\"red\"}";
        var serializedPayload = JsonConvert.SerializeObject(payload);

        // Assert
        Assert.Equal(expectedJsonString, serializedPayload);
    }

    [Fact]
    public void TestEndGamePayloadDeserialization()
    {
        // Arrange
        var jsonString = "{\"winner\":\"red\"}";

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
        var expectedJsonString = "{\"winner\":\"red\"}";
        var serializedPayload = JsonConvert.SerializeObject(payload);

        // Assert
        Assert.Equal(expectedJsonString, serializedPayload);
    }

    [Fact]
    public void TestGiveInfoForwardedPayloadDeserialization()
    {
        // Arrange
        var jsonString = "{\"respondingID\":1,\"distances\":[1, 2, 0, 1]," +
            "\"redTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]," +
            "\"blueTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]}";
        int x = 2, y = 2;
        GoalInfo[] expectedRedTeamGoalAreaInformations = new GoalInfo[x * y];
        GoalInfo[] expectedBlueTeamGoalAreaInformations = new GoalInfo[x * y];

        // Act
        expectedRedTeamGoalAreaInformations[0] = GoalInfo.IDK;
        expectedRedTeamGoalAreaInformations[1] = GoalInfo.IDK;
        expectedRedTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        expectedRedTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        expectedBlueTeamGoalAreaInformations[0] = GoalInfo.IDK;
        expectedBlueTeamGoalAreaInformations[1] = GoalInfo.IDK;
        expectedBlueTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        expectedBlueTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;
        var deserializedObject = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(jsonString);

        // Assert
        Assert.Equal(expectedRedTeamGoalAreaInformations, deserializedObject.RedTeamGoalAreaInformations);
        Assert.Equal(expectedBlueTeamGoalAreaInformations, deserializedObject.BlueTeamGoalAreaInformations);
    }

    [Fact]
    public void TestGiveInfoForwardedPayloadSerialization()
    {
        // Arrange
        int width = 2;
        int height = 2;
        var payload = new GiveInfoForwardedPayload()
        {
            RespondingID = 1,
        };

        payload.Distances = new int[width * height];
        payload.RedTeamGoalAreaInformations = new GoalInfo[width * height];
        payload.BlueTeamGoalAreaInformations = new GoalInfo[width * height];
        payload.Distances[0] = 1;
        payload.Distances[1] = 0;
        payload.Distances[1] = 2;
        payload.Distances[3] = 1;

        payload.RedTeamGoalAreaInformations[0] = GoalInfo.IDK;
        payload.RedTeamGoalAreaInformations[1] = GoalInfo.IDK;
        payload.RedTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        payload.RedTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        payload.BlueTeamGoalAreaInformations[0] = GoalInfo.IDK;
        payload.BlueTeamGoalAreaInformations[1] = GoalInfo.IDK;
        payload.BlueTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        payload.BlueTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        // Act
        var expectedJsonString = "{\"respondingID\":1,\"distances\":[1,2,0,1]," +
            "\"redTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]," +
            "\"blueTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]}";
        var serializedPayload = JsonConvert.SerializeObject(payload);

        // Assert
        Assert.Equal(expectedJsonString, serializedPayload);
    }

    [Fact]
    public void TestGiveInfoPayloadDeserialization()
    {
        // Arrange
        var jsonString = "{\"Distances\":[1,2,0,1],\"RespondToId\":1," +
            "\"redTeamGoalAreaInformations\":[\"IdK\",\"IdK\",\"N\",\"G\"]," +
            "\"blueTeamGoalAreaInformations\":[\"IdK\",\"IdK\",\"N\",\"G\"]}";
        int x = 2, y = 2;
        GoalInfo[] expectedRedTeamGoalAreaInformations = new GoalInfo[x * y];
        GoalInfo[] expectedBlueTeamGoalAreaInformations = new GoalInfo[x * y];

        // Act
        expectedRedTeamGoalAreaInformations[0] = GoalInfo.IDK;
        expectedRedTeamGoalAreaInformations[1] = GoalInfo.IDK;
        expectedRedTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        expectedRedTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        expectedBlueTeamGoalAreaInformations[0] = GoalInfo.IDK;
        expectedBlueTeamGoalAreaInformations[1] = GoalInfo.IDK;
        expectedBlueTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        expectedBlueTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;
        var deserializedObject = JsonConvert.DeserializeObject<GiveInfoPayload>(jsonString);

        // Assert
        Assert.Equal(expectedRedTeamGoalAreaInformations, deserializedObject.RedTeamGoalAreaInformations);
        Assert.Equal(expectedBlueTeamGoalAreaInformations, deserializedObject.BlueTeamGoalAreaInformations);
    }

    [Fact]
    public void TestGiveInfoPayloadSerialization()
    {
        // Arrange
        int width = 2;
        int height = 2;
        var payload = new GiveInfoPayload()
        {
            RespondToID = 1,
        };
        payload.Distances = new int[width * height];
        payload.RedTeamGoalAreaInformations = new GoalInfo[width * height];
        payload.BlueTeamGoalAreaInformations = new GoalInfo[width * height];
        payload.Distances[0] = 1;
        payload.Distances[1] = 0;
        payload.Distances[2] = 2;
        payload.Distances[3] = 1;

        payload.RedTeamGoalAreaInformations[0] = GoalInfo.IDK;
        payload.RedTeamGoalAreaInformations[1] = GoalInfo.IDK;
        payload.RedTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        payload.RedTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        payload.BlueTeamGoalAreaInformations[0] = GoalInfo.IDK;
        payload.BlueTeamGoalAreaInformations[1] = GoalInfo.IDK;
        payload.BlueTeamGoalAreaInformations[2] = GoalInfo.DiscoveredNotGoal;
        payload.BlueTeamGoalAreaInformations[3] = GoalInfo.DiscoveredGoal;

        // Act
        var expectedJsonString = "{\"distances\":[1,0,2,1],\"respondToID\":1," +
            "\"redTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]," +
            "\"blueTeamGoalAreaInformations\":[\"IDK\",\"IDK\",\"N\",\"G\"]}";
        var serializedPayload = JsonConvert.SerializeObject(payload);

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
        var serializedPayload = JsonConvert.SerializeObject(payload);

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
        var serializedPayload = JsonConvert.SerializeObject(payload);

        // Assert
        Assert.Equal(expectedJsonString, serializedPayload);
    }
}
