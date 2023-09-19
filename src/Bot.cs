using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace DiscordBot;

public class Bot {
    private DiscordSocketClient? client;

    private SocketTextChannel? chatChannel;
    private SocketTextChannel? consoleChannel;

    private DiscordBotMod Mod { get; }

    public Bot(DiscordBotMod mod) {
        Mod = mod;
    }

    public async Task Connect() {
        client = new DiscordSocketClient();
        client.Log += ClientLogToConsole;

        await client.LoginAsync(TokenType.Bot, Mod.Config?.Token ?? "");
        await client.StartAsync();

        var ready = new TaskCompletionSource<bool>();
        client.Disconnected += _ => {
            ready.TrySetResult(false);
            return Task.CompletedTask;
        };
        client.Ready += () => {
            ready.SetResult(true);
            return Task.CompletedTask;
        };
        if (!await ready.Task) {
            client = null;
            return;
        }

        chatChannel = client.GetChannel(Mod.Config?.ChatChannel ?? 0) as SocketTextChannel;
        consoleChannel = client.GetChannel(Mod.Config?.ConsoleChannel ?? 0) as SocketTextChannel;

        client.MessageReceived += DiscordMessageReceived;
    }

    private Task DiscordMessageReceived(SocketMessage arg) {
        if (client?.CurrentUser.Id == arg.Author.Id) {
            return Task.CompletedTask;
        }

        ulong channelId = arg.Channel.Id;
        if (chatChannel?.Id == channelId) {
            Mod.Api?.SendMessageToGroup(GlobalConstants.GeneralChatGroup, $"[{arg.Author.Username}]: {arg.Content}", EnumChatType.Notification);
        }
        else if (consoleChannel?.Id == channelId) {
            // todo - perform command in console
            //Mod.Api?.SendMessageToGroup(GlobalConstants.GeneralChatGroup, $"[{arg.Author.Username}]: {arg.Content}", EnumChatType.Notification);
        }

        return Task.CompletedTask;
    }

    public void OnLoggerEntryAdded(EnumLogType logType, string message, object[] args) {
        consoleChannel?.SendMessageAsync(string.Format(message, args));
    }

    private Task ClientLogToConsole(LogMessage msg) {
        switch (msg.Severity) {
            case LogSeverity.Critical or LogSeverity.Error:
                Mod.Logger.Error(msg.ToString());
                break;
            case LogSeverity.Warning:
                Mod.Logger.Warning(msg.ToString());
                break;
            case LogSeverity.Info:
                Mod.Logger.Event(msg.ToString());
                break;
            case LogSeverity.Verbose or LogSeverity.Debug:
                /* do nothing */
                break;
        }

        return Task.CompletedTask;
    }

    public void OnShutdown() {
        chatChannel?.SendMessageAsync(Lang.Get("discordbot:server-stopped")).Wait();
    }

    public void OnRunGame() {
        UpdatePresence();

        chatChannel?.SendMessageAsync(Lang.Get("discordbot:server-started"));
    }

    public void OnPlayerDeath(IServerPlayer player, DamageSource? damageSource) {
        string message = (damageSource?.Source ?? EnumDamageSource.Suicide) switch {
            EnumDamageSource.Block => "{0} was killed by a block.",
            EnumDamageSource.Player => "{0} was killed by {1}.",
            EnumDamageSource.Entity => "{0} was killed by a {1}.",
            EnumDamageSource.Fall => "{0} fell too far.",
            EnumDamageSource.Drown => "{0} drowned.",
            EnumDamageSource.Explosion => "{0} blew up.",
            EnumDamageSource.Suicide => "{0} couldn't take it anymore.",
            _ => "{0}'s death is a mystery."
        };

        string sourceEntity = damageSource?.SourceEntity?.GetName() ?? "null";
        chatChannel?.SendMessageAsync(string.Format(message, player.PlayerName, sourceEntity));
    }

    public void OnPlayerNowPlaying(IServerPlayer player) {
        UpdatePresence();
        chatChannel?.SendMessageAsync(Lang.Get("discordbot:player-joined", player.PlayerName));
    }

    public void OnPlayerDisconnect(IServerPlayer player) {
        UpdatePresence(-1); // todo
        chatChannel?.SendMessageAsync(Lang.Get("discordbot:player-left", player.PlayerName));
    }

    private async void UpdatePresence(int adjust = 0) {
        if (client == null) {
            return;
        }

        int count = Mod.Api?.World.AllOnlinePlayers.Length ?? 0 + adjust;
        await client.SetGameAsync($"with {count} player{(count != 1 ? "s" : "")}");
    }

    public void OnPlayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed) {
        string stripped = Regex.Replace(message, @"^((<.*>)?[^<>:]+:(</[^ ]*>)?) (.*)$", "$4");
        chatChannel?.SendMessageAsync($"**[{player.PlayerName}]** {stripped}", allowedMentions: AllowedMentions.None);
    }

    public void Dispose() {
        chatChannel = null;
        consoleChannel = null;

        client?.LogoutAsync().Wait();
        client?.Dispose();

        client = null;
    }
}
