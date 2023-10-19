using System.Reflection;
using HarmonyLib;
using Vintagestory.GameContent;

namespace DiscordBot.Patches;

public class SystemTemporalStabilityPatches {
    public static bool ScrapeTemporalStormBroadcast { get; private set; }
    
    protected internal SystemTemporalStabilityPatches(Harmony harmony) {
        _ = new OnTempStormTickPatch(harmony);
    }

    private class OnTempStormTickPatch {
        public OnTempStormTickPatch(Harmony harmony) {
            harmony.Patch(typeof(SystemTemporalStability).GetMethod("onTempStormTick", BindingFlags.Instance | BindingFlags.NonPublic),
                prefix: GetType().GetMethod("Prefix"),
                postfix: GetType().GetMethod("Postfix"));
        }

        public static void Prefix() {
            ScrapeTemporalStormBroadcast = true;
        }

        public static void Postfix() {
            ScrapeTemporalStormBroadcast = false;
        }
    }
}
