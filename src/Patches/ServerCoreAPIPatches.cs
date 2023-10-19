using System.Reflection;
using HarmonyLib;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class ServerCoreApiPatches {
    protected internal ServerCoreApiPatches(Harmony harmony) {
        _ = new BroadcastMessageToAllGroupsPatch(harmony);
    }

    private class BroadcastMessageToAllGroupsPatch {
        public BroadcastMessageToAllGroupsPatch(Harmony harmony) {
            harmony.Patch(typeof(ServerCoreAPI).GetMethod("BroadcastMessageToAllGroups", BindingFlags.Instance | BindingFlags.Public),
                postfix: GetType().GetMethod("Postfix"));
        }

        public static void Postfix(string message) {
            if (SystemTemporalStabilityPatches.ScrapeTemporalStormBroadcast) {
                DiscordBotMod.Bot?.OnTemporalStormAnnounce(message);
            }
        }
    }
}
