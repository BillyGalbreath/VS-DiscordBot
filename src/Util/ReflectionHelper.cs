using System.Reflection;

namespace DiscordBot.Util;

public static class ReflectionHelper {
    public static T? GetField<T>(this object obj, string name) {
        return (T?)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
    }
}
