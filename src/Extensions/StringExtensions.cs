using System;

namespace DiscordBot.Extensions;

public static class StringExtensions {
    private static readonly PluralFormatProvider PluralFormatProvider = new();

    public static string Format(this string format, params object?[] args) {
        return string.Format(PluralFormatProvider, format, args);
    }
}

public class PluralFormatProvider : IFormatProvider, ICustomFormatter {
    public object GetFormat(Type? formatType) {
        return this;
    }

    public string Format(string? format, object? arg, IFormatProvider? formatProvider) {
        if (format == null) {
            return $"{arg}";
        }

        if (!format.Contains(';')) {
            return string.Format($"{{0:{format}}}", arg);
        }

        int i = (int)(arg ?? 0) == 1 ? 0 : 1;
        return $"{arg} {format.Split(';')[i]}";
    }
}
