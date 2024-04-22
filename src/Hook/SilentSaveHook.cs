using Vintagestory.API.Common;

namespace DiscordBot.Hook;

public static class SilentSaveHook {
    private static ModSystem? _mod;
    private static long _lastCheck;

    private static ModSystem? CachedMod(ICoreAPI api) {
        long now = api.World.ElapsedMilliseconds;
        if (now - _lastCheck < 5000) {
            return _mod;
        }

        _mod ??= api.ModLoader.GetModSystem("SilentSave.SilentSaveMod");
        _lastCheck = now;

        return _mod;
    }

    public static bool SilentSaveInProgress(this ICoreAPI api) {
        return (bool)(CachedMod(api)?.GetType().GetMethod("SaveInProgress")?.Invoke(_mod, null) ?? false);
    }
}
