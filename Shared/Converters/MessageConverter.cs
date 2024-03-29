﻿using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Messages;
using Shared.Payloads;

namespace Shared.Converters;

public class MessageConverter : JsonConverter<Message>
{
    public override Message ReadJson(JsonReader reader, Type objectType, Message existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        JObject jsonObject = JObject.Load(reader);
        Message message = new Message
        {
            MessageID = jsonObject["messageID"].ToObject<int>().ToMessageIDEnum()
        };
        var agentID = jsonObject["agentID"];
        if (agentID != null)
        {
            message.AgentID = agentID.ToObject<int?>();
        }

        var payloadObject = jsonObject["payload"];
        if (payloadObject != null)
        {
            Type type = message.MessageID.GetPayloadType();
            message.Payload = (Payload)payloadObject.ToObject(type);
        }

        return message;
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, Message value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
