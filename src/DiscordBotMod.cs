using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DiscordBot;

public class DiscordBotMod : ModSystem {
    private readonly Bot bot;

    public ICoreServerAPI? Api { get; private set; }

    public Config? Config { get; private set; }

    public ILogger Logger => Mod.Logger;

    public DiscordBotMod() {
        bot = new Bot(this);
    }

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartPre(ICoreAPI api) {
        Config = api.LoadModConfig<Config>($"{Mod.Info.ModID}.json");
        if (Config != null) {
            return;
        }

        Config = new Config();
        api.StoreModConfig(Config, $"{Mod.Info.ModID}.json");
    }

    public override void StartServerSide(ICoreServerAPI api) {
        Api = api;

        bot.Connect().Wait();

        Api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, bot.OnRunGame);
        Api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, bot.OnShutdown);

        Api.Event.PlayerNowPlaying += bot.OnPlayerNowPlaying;
        Api.Event.PlayerDisconnect += bot.OnPlayerDisconnect;
        Api.Event.PlayerChat += bot.OnPlayerChat;
        Api.Event.PlayerDeath += bot.OnPlayerDeath;

        Api.Server.Logger.EntryAdded += bot.OnLoggerEntryAdded;
    }

    public override void Dispose() {
        if (Api != null) {
            Api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, bot.OnRunGame);
            Api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, bot.OnShutdown);

            Api.Event.PlayerNowPlaying -= bot.OnPlayerNowPlaying;
            Api.Event.PlayerDisconnect -= bot.OnPlayerDisconnect;
            Api.Event.PlayerChat -= bot.OnPlayerChat;
            Api.Event.PlayerDeath -= bot.OnPlayerDeath;

            Api.Server.Logger.EntryAdded -= bot.OnLoggerEntryAdded;
        }

        bot.Dispose();
    }
}
