using System.Reflection;

namespace DiscordBot.Extensions;

public static class ReflectionExtensions {
    public static T? GetField<T>(this object obj, string name) {
        return (T?)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
    }
}
