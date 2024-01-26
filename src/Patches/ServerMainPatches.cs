using System.Collections.Generic;
using System.Reflection;
using DiscordBot.Extensions;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.Server;

namespace DiscordBot.Patches;

public class ServerMainPatches {
    protected internal static readonly List<string> ConnectedPlayerUuids = new();

    protected internal ServerMainPatches(Harmony harmony) {
        _ = new HandleClientLoadedPatch(harmony);
        _ = new DisconnectPlayerPatch(harmony);
    }

    private class HandleClientLoadedPatch {
        public HandleClientLoadedPatch(Harmony harmony) {
            harmony.Patch(typeof(ServerMain).GetMethod("HandleClientLoaded", BindingFlags.Instance | BindingFlags.NonPublic),
                prefix: GetType().GetMethod("Prefix"));
        }

        public static void Prefix(ConnectedClient? client) {
            ServerPlayer? player = client?.GetField<ServerPlayer>("Player");
            if (player == null) {
                return;
            }

            ConnectedPlayerUuids.Add(player.PlayerUID);
            DiscordBotMod.Bot?.OnPlayerConnect(player, Lang.Get("{0} joined. Say hi :)", player.PlayerName));
        }
    }

    private class DisconnectPlayerPatch {
        public DisconnectPlayerPatch(Harmony harmony) {
            harmony.Patch(typeof(ServerMain).GetMethod("DisconnectPlayer", BindingFlags.Instance | BindingFlags.Public),
                postfix: GetType().GetMethod("Postfix"));
        }

        public static void Postfix(ConnectedClient? client, string? othersKickmessage) {
            ServerPlayer? player = client?.GetField<ServerPlayer>("Player");
            if (player != null && ConnectedPlayerUuids.Remove(player.PlayerUID)) {
                DiscordBotMod.Bot?.OnPlayerDisconnect(player, othersKickmessage);
            }
        }
    }
}
