using System.Collections.Generic;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using Xunit;

namespace Shared.Tests
{
    public class PlayerMessageTests
    {
        [Fact]
        public void TestPlayerMessageDeserialization()
        {
            // Arrange
            Dictionary<int, PlayerMessageId> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var jsonString = "{\"messageId\":" + msg.Key + ",\"PlayerId\":0,\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<PlayerMessage>(jsonString);

                // Assert
                Assert.Equal(msg.Value, deserializedObject.MessageId);
            }
        }

        [Fact]
        public void TestPlayerMessageSerialization()
        {
            // Arrange
            Dictionary<int, PlayerMessageId> playerMessages = GetPlayerMessageMapping();

            // Act
            foreach (var msg in playerMessages)
            {
                var obj = new PlayerMessage
                {
                    MessageId = msg.Value,
                    Payload = new EmptyAnswerPayload().Serialize(),
                };

                var expectedJsonString = "{\"MessageId\":" + msg.Key + ",\"PlayerId\":0,\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<int, PlayerMessageId> GetPlayerMessageMapping()
        {
            return new Dictionary<int, PlayerMessageId>()
            {
                { 0, PlayerMessageId.Unknown },
                { 1, PlayerMessageId.CheckPiece },
                { 2, PlayerMessageId.PieceDestruction },
                { 3, PlayerMessageId.Discover },
                { 4, PlayerMessageId.GiveInfo },
                { 5, PlayerMessageId.BegForInfo },
                { 6, PlayerMessageId.JoinTheGame },
                { 7, PlayerMessageId.Move },
                { 8, PlayerMessageId.Pick },
                { 9, PlayerMessageId.Put },
            };
        }
    }
}
