using System.Diagnostics.CodeAnalysis;
using DiscordBot.Extensions;
using HarmonyLib;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class PlayerDeathPatch {
    public PlayerDeathPatch(Harmony harmony) {
        harmony.Patch(typeof(ServerSystemEntitySimulation).GetMethod("GetDeathMessage", HarmonyPatches.Flags),
            postfix: GetType().GetMethod("GetDeathMessagePostfix"));
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void GetDeathMessagePostfix(ConnectedClient? client, string __result) {
        ServerPlayer? player = client?.GetField<ServerPlayer>("Player");
        if (player != null) {
            DiscordBotMod.Bot?.OnPlayerDeath(player, __result);
        }
    }
}
