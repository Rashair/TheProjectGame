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
            Dictionary<int, PlayerMessageID> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var jsonString = "{\"messageID\":" + msg.Key + ",\"PlayerID\":0,\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<PlayerMessage>(jsonString);

                // Assert
                Assert.Equal(msg.Value, deserializedObject.MessageID);
            }
        }

        [Fact]
        public void TestPlayerMessageSerialization()
        {
            // Arrange
            Dictionary<int, PlayerMessageID> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var obj = new PlayerMessage
                {
                    MessageID = msg.Value,
                    Payload = new EmptyAnswerPayload().Serialize(),
                };

                var expectedJsonString = "{\"MessageID\":" + msg.Key + ",\"PlayerID\":0,\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<int, PlayerMessageID> GetPlayerMessageMapping()
        {
            return new Dictionary<int, PlayerMessageID>()
            {
                { 0, PlayerMessageID.Unknown },
                { 1, PlayerMessageID.CheckPiece },
                { 2, PlayerMessageID.PieceDestruction },
                { 3, PlayerMessageID.Discover },
                { 4, PlayerMessageID.GiveInfo },
                { 5, PlayerMessageID.BegForInfo },
                { 6, PlayerMessageID.JoinTheGame },
                { 7, PlayerMessageID.Move },
                { 8, PlayerMessageID.Pick },
                { 9, PlayerMessageID.Put },
            };
        }
    }
}
