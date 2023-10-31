using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordBot.Command;
using DiscordBot.Config;
using DiscordBot.Extensions;
using DiscordBot.Util;
using SilentSave;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace DiscordBot;

[SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public class Bot {
    private DiscordSocketClient? client;

    private SocketTextChannel? chatChannel;
    private SocketTextChannel? consoleChannel;

    private MessageQueue? consoleQueue;

    private readonly CommandHandler commandHandler;

    private readonly DiscordWebhookClient?[] webhooks = new DiscordWebhookClient?[2];
    private int curWebhook = 1;
    private string? lastAuthor;

    private string? inviteUrl;

    public ICoreServerAPI Api { get; }
    public BotConfig Config { get; }
    public ILogger Logger { get; }

    public Bot(ILogger logger, ICoreServerAPI api) {
        Api = api;
        Logger = logger;

        Config = BotConfig.Reload();

        commandHandler = new CommandHandler(this);

        api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGame);
        api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, OnShutdown);

        api.Event.PlayerChat += OnPlayerChat;
        api.Server.Logger.EntryAdded += OnLoggerEntryAdded;

        api.ChatCommands.Create("discord")
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(_ =>
                TextCommandResult.Success(string.Format(Config.Messages.DiscordCommandOutput, inviteUrl))
            );
    }

    public async Task Connect() {
        try {
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

            await client.LoginAsync(TokenType.Bot, Config.Token);
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
            client.SlashCommandExecuted += commandHandler.HandleSlashCommands;

            await client.Rest.DeleteAllGlobalCommandsAsync();

            if (Config.ChatChannel != 0) {
                chatChannel = client.GetChannel(Config.ChatChannel) as SocketTextChannel;

                SocketTextChannel? channel = (SocketTextChannel?)(chatChannel is SocketThreadChannel thread ? thread.ParentChannel : chatChannel);

                SetupWebhooks(channel);

                client.MessageReceived += DiscordMessageReceived;

                commandHandler.RegisterAllCommands(channel!.Guild);

                if (Config.InGameInviteCode == "auto") {
                    IInviteMetadata invite = channel.CreateInviteAsync(maxAge: null).Result;
                    Config.InGameInviteCode = invite.Code;
                    BotConfig.Write(Config);
                    inviteUrl = invite.Url;
                }
                else {
                    inviteUrl = $"https://discord.gg/{Config.InGameInviteCode}";
                }
            }

            if (Config.ConsoleChannel != 0) {
                consoleChannel = client.GetChannel(Config.ConsoleChannel) as SocketTextChannel;
                consoleQueue = new MessageQueue(this);
            }
        }
        catch (Exception e) {
            Logger.Error(e);
        }
    }

    private void SetupWebhooks(SocketTextChannel? channel) {
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

    private void OnRunGame() {
        UpdatePresence();

        string format = Config.Messages.ServerStarted;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(text: format);
        }
    }

    private void OnShutdown() {
        string format = Config.Messages.ServerStopped;
        if (format.Length > 0) {
            SendMessageToDiscordChat(text: format, wait: true);
        }

        client?.StopAsync().Wait();
        client?.LogoutAsync().Wait();
    }

    public void OnPlayerConnect(IServerPlayer player, string? joinmessage = null) {
        Api.Event.RegisterCallback(_ => { UpdatePresence(); }, 1);

        string format = Config.Messages.PlayerJoined;
        if (format is { Length: > 0 } && joinmessage is { Length: > 0 }) {
            SendMessageToDiscordChat(0x00FF00, embed: format.Format(joinmessage, player.GetClass(), player.PlayerName), thumbnail: player.GetAvatar());
        }
    }

    public void OnPlayerDisconnect(IServerPlayer player, string? kickmessage = null) {
        Api.Event.RegisterCallback(_ => { UpdatePresence(); }, 1);

        string format = Config.Messages.PlayerLeft;
        if (format is { Length: > 0 } && kickmessage is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFF0000, embed: format.Format(kickmessage, player.PlayerName));
        }
    }

    public void OnPlayerDeath(IServerPlayer player, string deathMessage) {
        string format = Config.Messages.PlayerDeath;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0x121212, embed: format.Format(deathMessage, player.PlayerName));
        }
    }

    private void OnPlayerChat(IServerPlayer player, int channelId, ref string messageWithPrefix, ref string data, BoolRef consumed) {
        if (channelId != 0) {
            return; // ignore non-global chat
        }

        // for reference on data value format:
        // string data = $"from: {player.Entity.EntityId},withoutPrefix:{message}";
        string message = data[(data.IndexOf("x:", StringComparison.Ordinal) + 2)..];
        string toDiscord = message;

        if (Config.ParseUrlsInGameChat) {
            toDiscord = message.ParseForDiscord();
            string toGame = toDiscord.ParseForGame();
            messageWithPrefix = messageWithPrefix.Replace(message, toGame);
            data = $"from: {player.Entity.EntityId},withoutPrefix:{toGame}";
        }

        SendMessageToDiscordChat(text: toDiscord, username: player.PlayerName, avatar: player.GetAvatar());
    }

    public void OnCharacterSelection(IServerPlayer player) {
        string format = Config.Messages.PlayerChangedCharacter;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFFFF00, embed: format.Format(player.PlayerName, player.GetClass()), thumbnail: player.GetAvatar());
        }
    }

    public void OnTemporalStormAnnounce(string message) {
        string format = Config.Messages.TemporalStorm;
        if (format is { Length: > 0 }) {
            SendMessageToDiscordChat(0xFFFF00, embed: format.Format(message));
        }
    }

    private void OnLoggerEntryAdded(EnumLogType logType, string message, object[] args) {
        if (Api.ModLoader.GetModSystem<SilentSaveMod?>()?.SaveInProgress() ?? false) {
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
            string format = Config.Messages.PlayerChat;
            if (format.Length <= 0) {
                return Task.CompletedTask;
            }

            SendMessageToGameChat(format.Format(message.GetAuthor(), client.SanitizeMessage(message)));
        }
        else if (consoleChannel?.Id == message.Channel.Id) {
            Api.Event.EnqueueMainThreadTask(() => {
                ServerMain server = (ServerMain)Api.World;
                server.ReceiveServerConsole($"/{message}");
            }, "discordbot.console.command");
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

    internal void SendMessageToDiscordConsole(string message) {
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
        string format = Config.Messages.BotPresence;
        if (format.Length > 0) {
            client?.SetGameAsync(format.Format(Api.World.AllOnlinePlayers.Length, Api.Server.Config.MaxClients));
        }
    }

    public void Dispose() {
        Api.Event.PlayerChat -= OnPlayerChat;
        Api.Server.Logger.EntryAdded -= OnLoggerEntryAdded;

        if (client != null) {
            client.Log -= ClientLogToConsole;
            client.SlashCommandExecuted -= commandHandler.HandleSlashCommands;

            client.Dispose();
            client = null;
        }

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
