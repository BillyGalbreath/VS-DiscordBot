using System;

namespace DiscordBot.Util;

public class PluralFormatProvider : IFormatProvider, ICustomFormatter {
    public object GetFormat(Type? formatType) {
        return this;
    }

    public string Format(string? format, object? arg, IFormatProvider? formatProvider) {
        int value = (int)(arg ?? 0);
        return $"{value} {(format ?? "").Split(';')[value == 1 ? 0 : 1]}";
    }
}
