using System.Collections.Generic;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using Xunit;

namespace Shared.Tests
{
    public class GMMessageTests
    {
        [Fact]
        public void TestGMMessageDeserialization()
        {
            // Arrange
            Dictionary<int, GMMessageId> gmMessages = GetGMMessageMapping();

            // Act
            foreach (var msg in gmMessages)
            {
                var jsonString = "{\"MessageID\":" + msg.Key + ",\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<GMMessage>(jsonString);

                // Assert
                Assert.Equal(msg.Value, deserializedObject.MessageID);
            }
        }

        [Fact]
        public void TestGMMessageSerialization()
        {
            // Arrange
            Dictionary<int, GMMessageId> gmMessages = GetGMMessageMapping();
            int agentID = 3;

            // Act
            foreach (var msg in gmMessages)
            {
                var obj = new GMMessage
                {
                    MessageID = msg.Value,
                    AgentID = agentID,
                    Payload = new EmptyAnswerPayload(),
                };

                var expectedJsonString = "{\"MessageID\":" + msg.Key + ",\"AgentID\":" + agentID + ",\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<int, GMMessageId> GetGMMessageMapping()
        {
            return new Dictionary<int, GMMessageId>()
            {
                { 0, GMMessageId.Unknown },
                { 101, GMMessageId.CheckAnswer },
                { 102, GMMessageId.DestructionAnswer },
                { 103, GMMessageId.DiscoverAnswer },
                { 104, GMMessageId.EndGame },
                { 105, GMMessageId.StartGame },
                { 106, GMMessageId.BegForInfoForwarded },
                { 107, GMMessageId.JoinTheGameAnswer },
                { 108, GMMessageId.MoveAnswer },
                { 109, GMMessageId.PickAnswer },
                { 110, GMMessageId.PutAnswer },
                { 111, GMMessageId.GiveInfoForwarded },
                { 901, GMMessageId.InvalidMoveError },
                { 902, GMMessageId.PickError },
                { 903, GMMessageId.PutError },
                { 904, GMMessageId.NotWaitedError },
                { 905, GMMessageId.UnknownError },
            };
        }
    }
}
