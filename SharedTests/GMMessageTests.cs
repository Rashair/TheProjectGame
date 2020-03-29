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
            Dictionary<int, GMMessageID> gmMessages = GetGMMessageMapping();

            // Act
            foreach (var msg in gmMessages)
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
            Dictionary<int, GMMessageID> gmMessages = GetGMMessageMapping();

            // Act
            foreach (var msg in gmMessages)
            {
                var obj = new GMMessage
                {
                    Id = msg.Value,
                    Payload = new EmptyAnswerPayload().Serialize(),
                };

                var expectedJsonString = "{\"Id\":" + msg.Key + ",\"Payload\":\"{}\"}";
                var serializedObject = JsonConvert.SerializeObject(obj);

                // Assert
                Assert.Equal(expectedJsonString, serializedObject);
            }
        }

        private Dictionary<int, GMMessageID> GetGMMessageMapping()
        {
            return new Dictionary<int, GMMessageID>()
            {
                { 0, GMMessageID.Unknown },
                { 101, GMMessageID.CheckAnswer },
                { 102, GMMessageID.DestructionAnswer },
                { 103, GMMessageID.DiscoverAnswer },
                { 104, GMMessageID.EndGame },
                { 105, GMMessageID.StartGame },
                { 106, GMMessageID.BegForInfoForwarded },
                { 107, GMMessageID.JoinTheGameAnswer },
                { 108, GMMessageID.MoveAnswer },
                { 109, GMMessageID.PickAnswer },
                { 110, GMMessageID.PutAnswer },
                { 111, GMMessageID.GiveInfoForwarded },
                { 901, GMMessageID.InvalidMoveError },
                { 902, GMMessageID.PickError },
                { 903, GMMessageID.PutError },
                { 904, GMMessageID.NotWaitedError },
                { 905, GMMessageID.UnknownError },
            };
        }
    }
}
