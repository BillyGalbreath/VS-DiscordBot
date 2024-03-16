using Vintagestory.API.Common;

namespace DiscordBot.Hook;

public static class SilentSaveHook {
    private static ModSystem? _mod;
    private static long _lastCheck;

    private static ModSystem? CachedMod(ICoreAPI api) {
        long now = api.World.ElapsedMilliseconds;
        if (_lastCheck > 0 && now - _lastCheck < 5000) {
            return _mod;
        }

        _mod ??= api.ModLoader.GetModSystem("SilentSave.SilentSaveMod");
        _lastCheck = now;

        return _mod;
    }

    public static bool SilentSaveInProgress(this ICoreAPI api) {
        ModSystem? cached = CachedMod(api);
        return cached != null && ((SilentSave.SilentSaveMod)cached).SaveInProgress();
    }
}
