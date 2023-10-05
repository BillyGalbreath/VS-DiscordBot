using System.Collections.Generic;
using System.Reflection;
using DiscordBot.Util;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace DiscordBot;

[HarmonyPatch]
public class DiscordBotMod : ModSystem {
    private static DiscordBotMod? instance;

    private readonly Bot bot;

    private Harmony? harmony;
    private readonly List<string> connectedPlayerUuids = new();
    private readonly List<string> characterSelectCache = new();
    private bool scrapeTemporalStormBroadcast;

    public ICoreServerAPI? Api { get; private set; }
    public Config.Config Config { get; private set; } = new();
    public ILogger Logger => Mod.Logger;

    public DiscordBotMod() {
        instance = this;
        bot = new Bot(this);
    }

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        Api = api;

        harmony = new Harmony(Mod.Info.ModID);
        harmony.Patch(typeof(ServerMain).GetMethod("HandleClientLoaded", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(DiscordBotMod).GetMethod("PreHandleClientLoaded"));
        harmony.Patch(typeof(ServerMain).GetMethod("DisconnectPlayer", BindingFlags.Instance | BindingFlags.Public),
            postfix: typeof(DiscordBotMod).GetMethod("PostDisconnectPlayer"));
        harmony.Patch(typeof(ServerSystemEntitySimulation).GetMethod("GetDeathMessage", BindingFlags.Instance | BindingFlags.NonPublic),
            postfix: typeof(DiscordBotMod).GetMethod("PostGetDeathMessage"));
        harmony.Patch(typeof(SystemTemporalStability).GetMethod("onTempStormTick", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(DiscordBotMod).GetMethod("PreOnTempStormTick"),
            postfix: typeof(DiscordBotMod).GetMethod("PostOnTempStormTick"));
        harmony.Patch(typeof(ServerCoreAPI).GetMethod("BroadcastMessageToAllGroups", BindingFlags.Instance | BindingFlags.Public),
            postfix: typeof(DiscordBotMod).GetMethod("PostBroadcastMessageToAllGroups"));
        harmony.Patch(typeof(CharacterSystem).GetMethod("onCharacterSelection", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(DiscordBotMod).GetMethod("PreOnCharacterSelection"),
            postfix: typeof(DiscordBotMod).GetMethod("PostOnCharacterSelection"));

        const string configFile = "discordbot.json";
        Config = Api.LoadModConfig<Config.Config>(configFile) ?? new Config.Config();
        Api.StoreModConfig(Config, configFile);

        bot.Connect().Wait();

        Api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, bot.OnRunGame);
        Api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, bot.OnShutdown);

        Api.Event.PlayerChat += bot.OnPlayerChat;

        Api.Server.Logger.EntryAdded += bot.OnLoggerEntryAdded;
    }

    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);
        harmony = null;

        if (Api != null) {
            Api.Event.PlayerChat -= bot.OnPlayerChat;

            Api.Server.Logger.EntryAdded -= bot.OnLoggerEntryAdded;
        }

        connectedPlayerUuids.Clear();

        bot.Dispose();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ServerMain), "HandleClientLoaded")]
    public static void PreHandleClientLoaded(ConnectedClient client) {
        ServerPlayer player = client.GetField<ServerPlayer>("Player")!;
        instance!.connectedPlayerUuids.Add(player.PlayerUID);
        instance.bot.OnPlayerConnect(player, Lang.Get("{0} joined. Say hi :)", player.PlayerName));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerMain), "DisconnectPlayer")]
    public static void PostDisconnectPlayer(ConnectedClient? client, string? othersKickmessage) {
        ServerPlayer player = client?.GetField<ServerPlayer>("Player")!;
        if (instance!.connectedPlayerUuids.Remove(player.PlayerUID)) {
            instance.bot.OnPlayerDisconnect(player, othersKickmessage);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerSystemEntitySimulation), "GetDeathMessage")]
    public static void PostGetDeathMessage(ConnectedClient? client, string __result) {
        ServerPlayer player = client?.GetField<ServerPlayer>("Player")!;
        instance!.bot.OnPlayerDeath(player, __result);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SystemTemporalStability), "onTempStormTick")]
    public static void PreOnTempStormTick() {
        instance!.scrapeTemporalStormBroadcast = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SystemTemporalStability), "onTempStormTick")]
    public static void PostOnTempStormTick() {
        instance!.scrapeTemporalStormBroadcast = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerCoreAPI), "BroadcastMessageToAllGroups")]
    public static void PostBroadcastMessageToAllGroups(string message) {
        if (instance!.scrapeTemporalStormBroadcast) {
            instance.bot.OnTemporalStormAnnounce(message);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterSystem), "onCharacterSelection")]
    public static void PreOnCharacterSelection(IServerPlayer fromPlayer, CharacterSelectionPacket p) {
        bool didSelectBefore = SerializerUtil.Deserialize(fromPlayer.GetModdata("createCharacter"), false);
        if (didSelectBefore && fromPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
            return;
        }

        if (p.DidSelect) {
            instance!.characterSelectCache.Add(fromPlayer.PlayerUID);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterSystem), "onCharacterSelection")]
    public static void PostOnCharacterSelection(IServerPlayer fromPlayer) {
        if (instance!.characterSelectCache.Remove(fromPlayer.PlayerUID)) {
            instance.bot.OnCharacterSelection(fromPlayer);
        }
    }
}
