using System.Reflection;
using DiscordBot.Extensions;
using HarmonyLib;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class ServerSystemEntitySimulationPatches {
    protected internal ServerSystemEntitySimulationPatches(Harmony harmony) {
        _ = new GetDeathMessagePatch(harmony);
    }

    private class GetDeathMessagePatch {
        public GetDeathMessagePatch(Harmony harmony) {
            harmony.Patch(typeof(ServerSystemEntitySimulation).GetMethod("GetDeathMessage", BindingFlags.Instance | BindingFlags.NonPublic),
                postfix: GetType().GetMethod("Postfix"));
        }

        public static void Postfix(ConnectedClient? client, string __result) {
            ServerPlayer player = client?.GetField<ServerPlayer>("Player")!;
            DiscordBotMod.Bot?.OnPlayerDeath(player, __result);
        }
    }
}
