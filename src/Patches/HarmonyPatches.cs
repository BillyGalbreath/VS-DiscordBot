using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;

namespace DiscordBot.Patches;

public class HarmonyPatches {
    public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly string _modId;

    private Harmony? _harmony;

    public HarmonyPatches(ModSystem mod) {
        _modId = mod.Mod.Info.ModID;
        _harmony = new Harmony(_modId);

        _ = new CharacterSelectPatch(_harmony);
        _ = new PlayerDeathPatch(_harmony);
        _ = new PlayerJoinLeavePatch(_harmony);
        _ = new TemporalStormAnnouncementPatch(_harmony);
    }

    public void Dispose() {
        _harmony?.UnpatchAll(_modId);
        _harmony = null;
    }
}
