using HarmonyLib;
using Vintagestory.API.Common;

namespace DiscordBot.Patches;

public class HarmonyPatches {
    private readonly string modId;

    private Harmony? harmony;

    public HarmonyPatches(ModSystem mod) {
        modId = mod.Mod.Info.ModID;
        harmony = new Harmony(modId);

        _ = new ServerMainPatches(harmony);
        _ = new ServerSystemEntitySimulationPatches(harmony);
        _ = new SystemTemporalStabilityPatches(harmony);
        _ = new ServerCoreApiPatches(harmony);
        _ = new CharacterSystemPatches(harmony);
    }

    public void Dispose() {
        harmony?.UnpatchAll(modId);
        harmony = null;

        ServerMainPatches.CONNECTED_PLAYER_UUIDS.Clear();
    }
}
