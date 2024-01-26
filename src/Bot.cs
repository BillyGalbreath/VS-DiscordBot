using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordBot.Command;
using DiscordBot.Config;
using DiscordBot.Extensions;
using DiscordBot.Hook;
using DiscordBot.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace DiscordBot;

public class Bot {
    private DiscordSocketClient? _client;

    private SocketTextChannel? _chatChannel;
    private SocketTextChannel? _consoleChannel;

    private MessageQueue? _consoleQueue;

    private readonly CommandHandler _commandHandler;

    private readonly DiscordWebhookClient?[] _webhooks = new DiscordWebhookClient?[2];
    private int _curWebhook = 1;
    private string? _lastAuthor;

    private string? _inviteUrl;

    public ICoreServerAPI Api { get; }
    public BotConfig Config { get; }
    public ILogger Logger { get; }

    public Bot(ILogger logger, ICoreServerAPI api) {
        Api = api;
        Logger = logger;

        Config = BotConfig.Reload();

        _commandHandler = new CommandHandler(this);

        api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGame);
        api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, OnShutdown);

        api.Event.PlayerChat += OnPlayerChat;
        api.Server.Logger.EntryAdded += OnLoggerEntryAdded;

        api.ChatCommands.Create("discord")
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(_ =>
                TextCommandResult.Success(string.Format(Config.Messages.DiscordCommandOutput, _inviteUrl))
            );
    }

    public async Task Connect() {
        try {
            _client = new DiscordSocketClient(new DiscordSocketConfig {
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

            await _client.LoginAsync(TokenType.Bot, Config.Token);
            await _client.StartAsync();

            TaskCompletionSource<bool> ready = new();
            _client.Disconnected += _ => {
                ready.TrySetResult(false);
                return Task.CompletedTask;
            };

            _client.Ready += () => {
                ready.SetResult(true);
                return Task.CompletedTask;
            };

            if (!await ready.Task) {
                _client = null;
                return;
            }

            _client.Log += ClientLogToConsole;
            _client.SlashCommandExecuted += _commandHandler.HandleSlashCommands;

            await _client.Rest.DeleteAllGlobalCommandsAsync();

            if (Config.ChatChannel != 0) {
                _chatChannel = _client.GetChannel(Config.ChatChannel) as SocketTextChannel;

                SocketTextChannel? channel = (SocketTextChannel?)(_chatChannel is SocketThreadChannel thread ? thread.ParentChannel : _chatChannel);

                SetupWebhooks(channel);

                _client.MessageReceived += DiscordMessageReceived;

                _commandHandler.RegisterAllCommands(channel!.Guild);

                if (Config.InGameInviteCode == "auto") {
                    IInviteMetadata invite = channel.CreateInviteAsync(maxAge: null).Result;
                    Config.InGameInviteCode = invite.Code;
                    BotConfig.Write(Config);
                    _inviteUrl = invite.Url;
                } else {
                    _inviteUrl = $"https://discord.gg/{Config.InGameInviteCode}";
                }
            }

            if (Config.ConsoleChannel != 0) {
                _consoleChannel = _client.GetChannel(Config.ConsoleChannel) as SocketTextChannel;
                _consoleQueue = new MessageQueue(this);
            }
        } catch (Exception e) {
            Logger.Error(e);
        }
    }

    private void SetupWebhooks(SocketTextChannel? channel) {
        foreach (IWebhook webhook in channel?.GetWebhooksAsync().Result ?? Enumerable.Empty<IWebhook>()) {
            switch (webhook.Name) {
                case "vs1":
                    _webhooks[0] = new DiscordWebhookClient(webhook);
                    break;
                case "vs2":
                    _webhooks[1] = new DiscordWebhookClient(webhook);
                    break;
            }
        }

        _webhooks[0] ??= new DiscordWebhookClient((channel as IIntegrationChannel)?.CreateWebhookAsync("vs1").Result);
        _webhooks[1] ??= new DiscordWebhookClient((channel as IIntegrationChannel)?.CreateWebhookAsync("vs2").Result);
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

        _client?.StopAsync().Wait();
        _client?.LogoutAsync().Wait();
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
        if (Api.SilentSaveInProgress()) {
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
                _consoleQueue?.Enqueue(string.Format($"[{logType}] {message}", args));
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
        if (_client?.ShouldIgnore(message) ?? true) {
            return Task.CompletedTask;
        }

        if (_chatChannel?.Id == message.Channel.Id) {
            string format = Config.Messages.PlayerChat;
            if (format.Length <= 0) {
                return Task.CompletedTask;
            }

            SendMessageToGameChat(format.Format(message.GetAuthor(), _client.SanitizeMessage(message)));
        } else if (_consoleChannel?.Id == message.Channel.Id) {
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

        if (_lastAuthor != username) {
            _lastAuthor = username;
            _curWebhook = (_curWebhook + 1) & 1;
        }

        DiscordWebhookClient? webhook = _webhooks[_curWebhook];

        if (webhook != null) {
            Task<ulong>? task = webhook.SendMessageAsync(
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
                username: username ?? _client?.CurrentUser.Username,
                avatarUrl: avatar ?? _client?.CurrentUser.GetAvatarUrl(),
                allowedMentions: AllowedMentions.None,
                threadId: _chatChannel is SocketThreadChannel thread ? thread.Id : null
            );
            if (wait) {
                task?.Wait();
            }
        } else {
            Task<RestUserMessage>? task = _chatChannel?.SendMessageAsync(
                text: $"{username ?? _client?.CurrentUser.Username}: {text}",
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
            _consoleChannel?.SendMessageAsync(message, allowedMentions: AllowedMentions.None).Wait();
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
            _client?.SetGameAsync(format.Format(Api.World.AllOnlinePlayers.Length, Api.Server.Config.MaxClients));
        }
    }

    public void Dispose() {
        Api.Event.PlayerChat -= OnPlayerChat;
        Api.Server.Logger.EntryAdded -= OnLoggerEntryAdded;

        if (_client != null) {
            _client.Log -= ClientLogToConsole;
            _client.SlashCommandExecuted -= _commandHandler.HandleSlashCommands;

            _client.Dispose();
            _client = null;
        }

        _consoleQueue?.Dispose();
        _consoleQueue = null;

        _webhooks[0]?.Dispose();
        _webhooks[0] = null;
        _webhooks[1]?.Dispose();
        _webhooks[1] = null;

        _chatChannel = null;
        _consoleChannel = null;
    }
}
