using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using DiscordBot.Util;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace DiscordBot;

public class Bot {
    private readonly PluralFormatProvider pfp = new();
    private readonly BotConfig config;

    private DiscordSocketClient? client;

    private SocketTextChannel? chatChannel;
    private SocketTextChannel? consoleChannel;

    private MessageQueue? consoleQueue;

    private readonly DiscordWebhookClient?[] webhooks = new DiscordWebhookClient?[2];
    private int curWebhook = 1;
    private string? lastAuthor;

    private ICoreServerAPI Api { get; }
    private ILogger Logger { get; }

    public Bot(ILogger logger, ICoreServerAPI api) {
        Api = api;
        Logger = logger;

        api.StoreModConfig(config = api.LoadModConfig<BotConfig>(BotConfig.File) ?? new BotConfig(), BotConfig.File);

        api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGame);
        api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, OnShutdown);

        api.Event.PlayerChat += OnPlayerChat;
        api.Server.Logger.EntryAdded += OnLoggerEntryAdded;
    }

    public async Task Connect() {
        client = new DiscordSocketClient(new DiscordSocketConfig {
            GatewayIntents =
                //GatewayIntents.AutoModerationActionExecution |
                //GatewayIntents.AutoModerationConfiguration |
                //GatewayIntents.GuildScheduledEvents |
                //GatewayIntents.DirectMessageTyping |
                //GatewayIntents.DirectMessageReactions |
                //GatewayIntents.DirectMessages |
                //GatewayIntents.GuildMessageTyping |
                //GatewayIntents.GuildMessageReactions |
                GatewayIntents.GuildMessages |
                //GatewayIntents.GuildVoiceStates |
                //GatewayIntents.GuildInvites |
                GatewayIntents.GuildWebhooks |
                //GatewayIntents.GuildIntegrations |
                //GatewayIntents.GuildEmojis |
                //GatewayIntents.GuildBans |
                GatewayIntents.Guilds |
                GatewayIntents.MessageContent |
                //GatewayIntents.GuildPresences |
                GatewayIntents.GuildMembers
        });

        await client.LoginAsync(TokenType.Bot, config.Token);
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

        client.Log += ClientLogToConsole;

        if (config.ChatChannel != 0) {
            chatChannel = client.GetChannel(config.ChatChannel) as SocketTextChannel;

            SetupWebhooks();

            client.MessageReceived += DiscordMessageReceived;
        }

        if (config.ConsoleChannel != 0) {
            consoleChannel = client.GetChannel(config.ConsoleChannel) as SocketTextChannel;

            consoleQueue = new MessageQueue();

            Api.Event.RegisterGameTickListener(_ => {
                foreach (string line in consoleQueue.Process()) {
                    string text = line;
                    while (text.Length > 0) {
                        if (text.Length > 2000) {
                            SendMessageToDiscordConsole(text[..2000]);
                            text = text[2000..];
                            continue;
                        }

                        SendMessageToDiscordConsole(text);
                        break;
                    }
                }
            }, 1000);
        }
    }

    private void SetupWebhooks() {
        SocketTextChannel? channel = (SocketTextChannel?)(chatChannel is SocketThreadChannel thread ? thread.ParentChannel : chatChannel);
        foreach (IWebhook webhook in channel?.GetWebhooksAsync().Result ?? Enumerable.Empty<IWebhook>()) {
            switch (webhook.Name) {
                case "vs1":
                    webhooks[0] = new DiscordWebhookClient(webhook);
                    break;
                case "vs2":
                    webhooks[1] = new DiscordWebhookClient(webhook);
                    break;
            }
        }

        webhooks[0] ??= new DiscordWebhookClient((channel as IIntegrationChannel)?.CreateWebhookAsync("vs1").Result);
        webhooks[1] ??= new DiscordWebhookClient((channel as IIntegrationChannel)?.CreateWebhookAsync("vs2").Result);
    }

    public void OnRunGame() {
        UpdatePresence();

        string format = config.Messages.ServerStarted;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(text: format);
        }
    }

    public void OnShutdown() {
        string format = config.Messages.ServerStopped;
        if (format.Length > 0) {
            SendMessageToDiscordChat(text: format, wait: true);
        }

        client?.StopAsync().Wait();
        client?.LogoutAsync().Wait();
    }

    public void OnPlayerConnect(IServerPlayer player, string? joinmessage = null) {
        Api.Event.RegisterCallback(_ => { UpdatePresence(); }, 1);

        string format = config.Messages.PlayerJoined;
        if (format is { Length: > 0 } && joinmessage is { Length: > 0 }) {
            SendMessageToDiscordChat(0x00FF00, embed: string.Format(format, joinmessage, player.GetClass(), player.PlayerName), thumbnail: player.GetAvatar());
        }
    }

    public void OnPlayerDisconnect(IServerPlayer player, string? kickmessage = null) {
        Api.Event.RegisterCallback(_ => { UpdatePresence(); }, 1);

        string format = config.Messages.PlayerLeft;
        if (format is { Length: > 0 } && kickmessage is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFF0000, embed: string.Format(format, kickmessage, player.PlayerName));
        }
    }

    public void OnPlayerDeath(IServerPlayer player, string deathMessage) {
        string format = config.Messages.PlayerDeath;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0x121212, embed: string.Format(format, deathMessage, player.PlayerName));
        }
    }

    public void OnPlayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed) {
        if (channelId != 0) {
            return; // ignore non-global chat
        }

        SendMessageToDiscordChat(text: Regex.Replace(message, @"^((<.*>)?[^<>:]+:(</[^ ]*>)?) (.*)$", "$4"),
            username: player.PlayerName, avatar: player.GetAvatar());
    }

    public void OnCharacterSelection(IServerPlayer player) {
        string format = config.Messages.PlayerChangedCharacter;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFFFF00, embed: string.Format(format, player.PlayerName, player.GetClass()), thumbnail: player.GetAvatar());
        }
    }

    public void OnTemporalStormAnnounce(string message) {
        string format = config.Messages.TemporalStorm;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFFFF00, embed: string.Format(format, message));
        }
    }

    public void OnLoggerEntryAdded(EnumLogType logType, string message, object[] args) {
        if (!(bool)(AccessTools.TypeByName("SilentSave.SilentSaveMod")?
                .GetField("allowLogger", BindingFlags.NonPublic | BindingFlags.Static)?
                .GetValue(null) ?? true)) {
            return;
        }

        switch (logType) {
            case EnumLogType.Chat:
            case EnumLogType.Event:
            case EnumLogType.StoryEvent:
            case EnumLogType.Notification:
            case EnumLogType.Warning:
            case EnumLogType.Error:
            case EnumLogType.Fatal:
                consoleQueue?.Enqueue(string.Format($"[{logType}] {message}", args));
                break;
            case EnumLogType.Build:
            case EnumLogType.VerboseDebug:
            case EnumLogType.Debug:
            case EnumLogType.Audit:
            case EnumLogType.Worldgen:
            default:
                break;
        }
    }

    private Task DiscordMessageReceived(SocketMessage message) {
        if (client?.ShouldIgnore(message) ?? true) {
            return Task.CompletedTask;
        }

        if (chatChannel?.Id == message.Channel.Id) {
            string format = config.Messages.PlayerChat;
            if (format.Length <= 0) {
                return Task.CompletedTask;
            }

            SendMessageToGameChat(string.Format(format, message.GetAuthor(), client.SanitizeMessage(message)));
        }
        else if (consoleChannel?.Id == message.Channel.Id) {
            ((ServerMain)Api.World).ReceiveServerConsole($"/{message}");
        }

        return Task.CompletedTask;
    }

    private Task ClientLogToConsole(LogMessage msg) {
        switch (msg.Severity) {
            case LogSeverity.Critical or LogSeverity.Error:
                Logger.Error(msg.Message ?? msg.Exception.Message);
                break;
            case LogSeverity.Warning:
                Logger.Warning(msg.Message ?? msg.Exception.Message);
                break;
            case LogSeverity.Info:
                Logger.Event(msg.Message ?? msg.Exception.Message);
                break;
            case LogSeverity.Verbose or LogSeverity.Debug:
                /* do nothing */
                break;
        }

        return Task.CompletedTask;
    }

    private void SendMessageToDiscordChat(uint color = 0x0, string text = "", string embed = "", string? username = null, string? avatar = null, string? thumbnail = null, bool wait = false) {
        if (text.Length <= 0 && embed.Length <= 0) {
            return;
        }

        if (lastAuthor != username) {
            lastAuthor = username;
            curWebhook = (curWebhook + 1) & 1;
        }

        DiscordWebhookClient? webhook = webhooks[curWebhook];

        if (webhook != null) {
            var task = webhook.SendMessageAsync(
                text: text,
                embeds: embed.Length <= 0
                    ? null
                    : new[] {
                        new EmbedBuilder()
                            .WithColor(color)
                            .WithDescription(embed)
                            .WithThumbnailUrl(thumbnail)
                            .Build()
                    },
                username: username ?? client?.CurrentUser.Username,
                avatarUrl: avatar ?? client?.CurrentUser.GetAvatarUrl(),
                allowedMentions: AllowedMentions.None,
                threadId: chatChannel is SocketThreadChannel thread ? thread.Id : null
            );
            if (wait) {
                task?.Wait();
            }
        }
        else {
            var task = chatChannel?.SendMessageAsync(
                text: $"{username ?? client?.CurrentUser.Username}: {text}",
                embed: embed.Length <= 0
                    ? null
                    : new EmbedBuilder()
                        .WithColor(color)
                        .WithDescription(embed)
                        .WithThumbnailUrl(thumbnail)
                        .Build(),
                allowedMentions: AllowedMentions.None
            );
            if (wait) {
                task?.Wait();
            }
        }
    }

    private void SendMessageToDiscordConsole(string message) {
        if (message.Length > 0) {
            consoleChannel?.SendMessageAsync(message, allowedMentions: AllowedMentions.None).Wait();
        }
    }

    private void SendMessageToGameChat(string message) {
        if (message.Length > 0) {
            Api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, message, EnumChatType.OthersMessage);
        }
    }

    private void UpdatePresence() {
        string format = config.Messages.BotPresence;
        if (format.Length > 0) {
            client?.SetGameAsync(string.Format(pfp, format, Api.World.AllOnlinePlayers.Length));
        }
    }

    public void Dispose() {
        Api.Event.PlayerChat -= OnPlayerChat;
        Api.Server.Logger.EntryAdded -= OnLoggerEntryAdded;

        client?.Dispose();
        client = null;

        consoleQueue?.Dispose();
        consoleQueue = null;

        webhooks[0]?.Dispose();
        webhooks[0] = null;
        webhooks[1]?.Dispose();
        webhooks[1] = null;

        chatChannel = null;
        consoleChannel = null;
    }
}
