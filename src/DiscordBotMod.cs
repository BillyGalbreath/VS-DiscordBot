using DiscordBot.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DiscordBot;

public class DiscordBotMod : ModSystem {
    public static Bot? Bot { get; private set; }

    private HarmonyPatches? _patches;

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        Bot = new Bot(Mod.Logger, api);
        _patches = new HarmonyPatches(this);

        Bot.Connect().Wait();
    }

    public override void Dispose() {
        _patches?.Dispose();
        _patches = null;

        Bot?.Dispose();
        Bot = null;
    }
}
