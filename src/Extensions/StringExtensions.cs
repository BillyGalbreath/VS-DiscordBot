using DiscordBot.Util;

namespace DiscordBot.Extensions;

public static class StringExtensions {
    private static readonly PluralFormatProvider PFP = new();

    public static string Format(this string format, params object?[] args) {
        return string.Format(PFP, format, args);
    }
}
