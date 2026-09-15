using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

#nullable enable

namespace DiceRoller.Core;

public static class SettingsCodec
{
    public static string Serialize(IReadOnlyDictionary<string, object> values)
    {
        var json = new JsonObject();
        foreach (var (key, value) in values)
        {
            json[key] = value switch
            {
                bool flag => JsonValue.Create(flag),
                float number => JsonValue.Create(number),
                double number => JsonValue.Create(number),
                int number => JsonValue.Create(number),
                string text => JsonValue.Create(text),
                _ => JsonValue.Create(value.ToString()),
            };
        }

        return json.ToJsonString();
    }

    public static Dictionary<string, object> Deserialize(string json)
    {
        var result = new Dictionary<string, object>();
        if (JsonNode.Parse(json) is not JsonObject root)
            return result;

        foreach (var (key, node) in root)
        {
            if (node is null)
                continue;

            result[key] = node.GetValueKind() switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => node.GetValue<string>(),
                JsonValueKind.Number => node.GetValue<double>(),
                _ => node.ToJsonString(),
            };
        }

        return result;
    }
}
