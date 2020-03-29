using System.Collections.Generic;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;
using Xunit;

namespace Shared.Tests
{
    public class PlayerMessageTests
    {
        [Fact]
        public void TestPlayerMessageDeserialization()
        {
            // Arrange
            Dictionary<string, PlayerMessageID> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var jsonString = "{\"messageID\":\"" + msg.Key + "\",\"PlayerID\":0,\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<PlayerMessage>(jsonString);

                // Assert
                Assert.Equal(msg.Value, deserializedObject.MessageID);
            }
        }

        [Fact]
        public void TestPlayerMessageSerialization()
        {
            // Arrange
            Dictionary<string, PlayerMessageID> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var obj = new PlayerMessage
                {
                    MessageID = msg.Value,
                    Payload = new EmptyAnswerPayload().Serialize(),
                };

                var expectedJsonString = "{\"messageID\":\"" + msg.Key + "\",\"PlayerID\":0,\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<string, PlayerMessageID> GetPlayerMessageMapping()
        {
            return new Dictionary<string, PlayerMessageID>()
            {
                { "Unknown", PlayerMessageID.Unknown },
                { "CheckPiece", PlayerMessageID.CheckPiece },
                { "PieceDestruction", PlayerMessageID.PieceDestruction },
                { "Discover", PlayerMessageID.Discover },
                { "GiveInfo", PlayerMessageID.GiveInfo },
                { "BegForInfo", PlayerMessageID.BegForInfo },
                { "JoinTheGame", PlayerMessageID.JoinTheGame },
                { "Move", PlayerMessageID.Move },
                { "Pick", PlayerMessageID.Pick },
                { "Put", PlayerMessageID.Put },
            };
        }
    }
}
