using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class TemporalStormAnnouncementPatch {
    private static bool _scrapeTemporalStormBroadcast;

    public TemporalStormAnnouncementPatch(Harmony harmony) {
        harmony.Patch(typeof(SystemTemporalStability).GetMethod("onTempStormTick", HarmonyPatches.Flags),
            prefix: GetType().GetMethod("OnTempStormTickPrefix"),
            postfix: GetType().GetMethod("OnTempStormTickPostfix"));
        harmony.Patch(typeof(ServerCoreAPI).GetMethod("BroadcastMessageToAllGroups", HarmonyPatches.Flags),
            postfix: GetType().GetMethod("BroadcastMessageToAllGroupsPostfix"));
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void OnTempStormTickPrefix() {
        _scrapeTemporalStormBroadcast = true;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void OnTempStormTickPostfix() {
        _scrapeTemporalStormBroadcast = false;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void BroadcastMessageToAllGroupsPostfix(string message) {
        if (_scrapeTemporalStormBroadcast) {
            DiscordBotMod.Bot?.OnTemporalStormAnnounce(message);
        }
    }
}
