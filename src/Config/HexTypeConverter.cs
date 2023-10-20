using System;
using System.Globalization;
using Discord;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DiscordBot.Config;

public class HexTypeConverter : IYamlTypeConverter {
    public bool Accepts(Type type) => type == typeof(Color);

    public object ReadYaml(IParser parser, Type type) {
        try {
            return new Color(uint.Parse(parser.Consume<Scalar>().Value[1..], NumberStyles.HexNumber));
        }
        catch (Exception) {
            return Color.Teal;
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type) {
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, ((Color?)value ?? Color.Teal).ToString()!, ScalarStyle.DoubleQuoted, true, false));
    }
}
