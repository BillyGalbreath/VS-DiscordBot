using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
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

        Api.Event.PlayerNowPlaying += bot.OnPlayerNowPlaying;
        Api.Event.PlayerDisconnect += bot.OnPlayerDisconnect;
        Api.Event.PlayerChat += bot.OnPlayerChat;
        //Api.Event.PlayerDeath += bot.OnPlayerDeath;

        Api.Server.Logger.EntryAdded += bot.OnLoggerEntryAdded;
    }

    public override void Dispose() {
        if (Api != null) {
            Api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, bot.OnRunGame);
            Api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, bot.OnShutdown);

            Api.Event.PlayerNowPlaying -= bot.OnPlayerNowPlaying;
            Api.Event.PlayerDisconnect -= bot.OnPlayerDisconnect;
            Api.Event.PlayerChat -= bot.OnPlayerChat;

            // event doesn't contain the death message
            // so we use harmony to inject into somewhere it does.
            //Api.Event.PlayerDeath -= bot.OnPlayerDeath;

            Api.Server.Logger.EntryAdded -= bot.OnLoggerEntryAdded;
        }

        bot.Dispose();

        harmony?.UnpatchAll(Mod.Info.ModID);
        harmony = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerSystemEntitySimulation), "GetDeathMessage")]
    public static void PostGetDeathMessage(string __result) {
        instance!.bot.OnPlayerDeath(__result);
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
            CHAR_SEL_CACHE.Add(fromPlayer.PlayerUID);
        }
    }

    private static readonly List<string> CHAR_SEL_CACHE = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterSystem), "onCharacterSelection")]
    public static void PostOnCharacterSelection(IServerPlayer fromPlayer) {
        if (!CHAR_SEL_CACHE.Remove(fromPlayer.PlayerUID)) {
            return;
        }

        string message = instance!.Config.Messages.PlayerChangedCharacter;
        if (message.Length > 0) {
            instance.bot.SendMessageToDiscordChat(0xFFFF00,
                embed: string.Format(message, fromPlayer.PlayerName, Bot.GetClass(fromPlayer)),
                thumbnail: Bot.GetAvatar(fromPlayer)
            );
        }
    }
}
