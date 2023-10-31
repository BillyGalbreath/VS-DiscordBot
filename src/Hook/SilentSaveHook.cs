using System;
using Vintagestory.API.Common;

namespace DiscordBot.Hook;

public static class SilentSaveHook {
    private static ModSystem? mod;
    private static long lastCheck;

    private static ModSystem? CachedMod(ICoreAPI api) {
        long now = Environment.TickCount;
        if (now - lastCheck < 1000) {
            return mod;
        }

        mod ??= api.ModLoader.GetModSystem("SilentSave.SilentSaveMod");
        lastCheck = now;

        return mod;
    }

    public static bool SilentSaveInProgress(this ICoreAPI api) {
        return (bool)(CachedMod(api)?.GetType().GetMethod("SaveInProgress")?.Invoke(mod, null) ?? false);
    }
}
