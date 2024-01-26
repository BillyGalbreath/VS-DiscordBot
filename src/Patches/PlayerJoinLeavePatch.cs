using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DiscordBot.Extensions;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class PlayerJoinLeavePatch {
    private static readonly List<string> ConnectedPlayerUuids = new();

    public PlayerJoinLeavePatch(Harmony harmony) {
        harmony.Patch(typeof(ServerMain).GetMethod("HandleClientLoaded", HarmonyPatches.Flags),
            prefix: GetType().GetMethod("HandleClientLoadedPrefix"));
        harmony.Patch(typeof(ServerMain).GetMethod("DisconnectPlayer", HarmonyPatches.Flags),
            postfix: GetType().GetMethod("DisconnectPlayerPostfix"));
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void HandleClientLoadedPrefix(ConnectedClient? client) {
        ServerPlayer? player = client?.GetField<ServerPlayer>("Player");
        if (player == null) {
            return;
        }

        ConnectedPlayerUuids.Add(player.PlayerUID);
        DiscordBotMod.Bot?.OnPlayerConnect(player, Lang.Get("{0} joined. Say hi :)", player.PlayerName));
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void DisconnectPlayerPostfix(ConnectedClient? client, string? othersKickmessage) {
        ServerPlayer? player = client?.GetField<ServerPlayer>("Player");
        if (player != null && ConnectedPlayerUuids.Remove(player.PlayerUID)) {
            DiscordBotMod.Bot?.OnPlayerDisconnect(player, othersKickmessage);
        }
    }
}
