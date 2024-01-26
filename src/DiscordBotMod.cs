using System.Diagnostics.CodeAnalysis;
using DiscordBot.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DiscordBot;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class DiscordBotMod : ModSystem {
    public static Bot? Bot { get; private set; }

    private HarmonyPatches? _harmony;

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        _harmony = new HarmonyPatches(this);

        Bot = new Bot(Mod.Logger, api);
        Bot.Connect().Wait();
    }

    public override void Dispose() {
        _harmony?.Dispose();
        _harmony = null;

        Bot?.Dispose();
        Bot = null;
    }
}
