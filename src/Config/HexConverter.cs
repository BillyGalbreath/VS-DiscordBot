using System;
using System.Globalization;
using Discord;
using Newtonsoft.Json;

namespace DiscordBot.Config;

public class HexConverter : JsonConverter<Color> {
    public override void WriteJson(JsonWriter writer, Color color, JsonSerializer serializer) {
        serializer.Serialize(writer, color.ToString());
    }

    public override Color ReadJson(JsonReader reader, Type type, Color existing, bool hasExisting, JsonSerializer serializer) {
        return new Color(uint.Parse(serializer.Deserialize<string>(reader)?[1..] ?? string.Empty, NumberStyles.HexNumber));
    }
}
