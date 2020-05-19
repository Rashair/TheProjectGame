using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using Xunit;

namespace Shared.Tests
{
    public class MessageTests
    {
        [Fact]
        public void TestMessageDeserialization()
        {
            // Arrange
            IEnumerable<MessageID> messages = GetMessages();

            // Act
            foreach (var id in messages)
            {
                var jsonString = "{\"messageId\":" + (int)id + ",\"AgentID\":0,\"Payload\":\"{}\"}";
                var deserializedObject = JsonConvert.DeserializeObject<Message>(jsonString);

                // Assert
                Assert.Equal(id, deserializedObject.MessageID);
            }
        }

        [Fact]
        public void TestMessageSerialization()
        {
            // Arrange
            IEnumerable<MessageID> messages = GetMessages();

            // Act
            foreach (var id in messages)
            {
                var obj = new Message
                {
                    MessageID = id,
                    Payload = new EmptyAnswerPayload(),
                };

                var expectedJsonString = "{\"messageID\":" + (int)id + ",\"agentID\":0,\"payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private IEnumerable<MessageID> GetMessages()
        {
            return Enum.GetValues(typeof(MessageID)).Cast<MessageID>();
        }
    }
}
