using HarmonyLib;
using Vintagestory.API.Common;

namespace DiscordBot.Patches;

public class HarmonyPatches {
    private readonly string _modId;

    private Harmony? _harmony;

    public HarmonyPatches(ModSystem mod) {
        _modId = mod.Mod.Info.ModID;
        _harmony = new Harmony(_modId);

        _ = new ServerMainPatches(_harmony);
        _ = new ServerSystemEntitySimulationPatches(_harmony);
        _ = new SystemTemporalStabilityPatches(_harmony);
        _ = new ServerCoreApiPatches(_harmony);
        _ = new CharacterSystemPatches(_harmony);
    }

    public void Dispose() {
        _harmony?.UnpatchAll(_modId);
        _harmony = null;

        ServerMainPatches.ConnectedPlayerUuids.Clear();
    }
}
