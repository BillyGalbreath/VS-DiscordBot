using System;

namespace DiscordBot.Util;

public class PluralFormatProvider : IFormatProvider, ICustomFormatter {
    public object GetFormat(Type? formatType) {
        return this;
    }

    public string Format(string? format, object? arg, IFormatProvider? formatProvider) {
        if (format == null) {
            return $"{arg}";
        }

        if (!format.Contains(';')) {
            // ReSharper disable FormatStringProblem
            return string.Format("{0:" + format + "}", arg);
        }

        int i = (int)(arg ?? 0) == 1 ? 0 : 1;
        return $"{arg} {format.Split(';')[i]}";
    }
}
