using System.Collections.Generic;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;
using Xunit;

namespace Shared.Tests
{
    public class GMMessageTests
    {
        [Fact]
        public void TestGMMessageDeserialization()
        {
            // Arrange
            Dictionary<string, GMMessageID> expectations = GetGMMessageMapping();

            // Act
            foreach (var msg in expectations)
            {
                var jsonString = "{\"id\":\"" + msg.Key + "\",\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<GMMessage>(jsonString);

                // Assert
                Assert.Equal(msg.Value, deserializedObject.Id);
            }
        }

        [Fact]
        public void TestGMMessageSerialization()
        {
            // Arrange
            Dictionary<string, GMMessageID> gmMessages = GetGMMessageMapping();

            // Act
            foreach (var msg in gmMessages)
            {
                var obj = new GMMessage
                {
                    Id = msg.Value,
                    Payload = new EmptyAnswerPayload().Serialize(),
                };

                var expectedJsonString = "{\"id\":\"" + msg.Key + "\",\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<string, GMMessageID> GetGMMessageMapping()
        {
            return new Dictionary<string, GMMessageID>()
            {
                { "Unknown", GMMessageID.Unknown },
                { "CheckAnswer", GMMessageID.CheckAnswer },
                { "DestructionAnswer", GMMessageID.DestructionAnswer },
                { "DiscoverAnswer", GMMessageID.DiscoverAnswer },
                { "EndGame", GMMessageID.EndGame },
                { "StartGame", GMMessageID.StartGame },
                { "BegForInfoForwarded", GMMessageID.BegForInfoForwarded },
                { "JoinTheGameAnswer", GMMessageID.JoinTheGameAnswer },
                { "MoveAnswer", GMMessageID.MoveAnswer },
                { "PickAnswer", GMMessageID.PickAnswer },
                { "PutAnswer", GMMessageID.PutAnswer },
                { "GiveInfoForwarded", GMMessageID.GiveInfoForwarded },
            };
        }
    }
}
