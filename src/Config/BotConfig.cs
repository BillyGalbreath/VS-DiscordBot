using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Discord;
using Vintagestory.API.Config;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordBot.Config;

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class BotConfig {
    private static readonly string FILENAME = Path.Combine(GamePaths.ModConfig, "discordbot.yml");

    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Your bot's secret token. Go to https://discord.com/developers/applications to set up your bot.\nAt a minimum bot requires all three privileged intents (https://discord.com/developers/docs/topics/gateway#privileged-intents),\nand bot permissions to manage webhooks (https://discord.com/developers/docs/getting-started#adding-scopes-and-bot-permissions)")]
    public string Token { get; private set; } = "your-bot-token";

    [YamlMember(Order = 1, Description = "Channel ID where you want player chat and events to go to/from.")]
    public ulong ChatChannel { get; private set; }

    [YamlMember(Order = 2, Description = "Channel ID where you want console to log to.\nWARNING: Protect this channel, as any messages sent to it will be run\nas commands directly in the console!\nLeave as 0 if you dont want this feature enabled.")]
    public ulong ConsoleChannel { get; private set; }

    [YamlMember(Order = 3, Description = "\nCustomizable messages.")]
    public ConfigMessages Messages = new();

    [YamlMember(Order = 4, Description = "\nDiscord slash command options.")]
    public ConfigCommands Commands = new();

    public class ConfigMessages {
        [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The server is fully started.")]
        public string ServerStarted = ":white_check_mark: **Server has started**";

        [YamlMember(Order = 1, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The server is shutting down.")]
        public string ServerStopped = ":octagonal_sign: **Server has stopped**";

        [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The bot's presence text. (the \"playing with ...\" text in the users list)\n\"{0}\" is for current player count and \"{1}\" is for maximum player count.")]
        public string BotPresence = "with {0:player;players}";

        [YamlMember(Order = 3, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has fully joined the server and is now in game.\n\"{0}\" is the player's name and \"{1}\" is the player's class.")]
        public string PlayerJoined = "**{0}**\n\n\nClass: _{1}_";

        [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has left the server.\n\"{0}\" is the disconnect message that was sent in-game and \"{1}\" is the player's name.")]
        public string PlayerLeft = "**{0}**";

        [YamlMember(Order = 5, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has died.\n\"{0}\" is the death message that was sent in-game and \"{1}\" is the player's name.")]
        public string PlayerDeath = "**{0}**";

        [YamlMember(Order = 6, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has chatted in the discord server. This is how it should look in the game.\n\"{0}\" is the player's name and \"{1}\" is the message sent.")]
        public string PlayerChat = "<strong>[{0}]:</strong> {1}";

        [YamlMember(Order = 7, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has changed their character/class.\n\"{0}\" is the player's name and \"{1}\" is the player's class.")]
        public string PlayerChangedCharacter = "**{0} updated their character**\n\n\nClass: _{1}_";

        [YamlMember(Order = 8, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A temporal storm message was sent in game.\n\"{0}\" is the message that was sent.")]
        public string TemporalStorm = "**:thunder_cloud_rain: {0}**";
    }

    public class ConfigCommands {
        [YamlMember(Order = 0, Description = "Options for the /players command to list currently online players.")]
        public ConfigPlayersCommand ConfigPlayers = new();

        public class ConfigPlayersCommand {
            [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed.\nSupports any six-digit color hex value with leading \"#\".")]
            public Color Color = 0x008080;

            [YamlMember(Order = 1, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed.\n\"{0}\" is for current player count and \"{1}\" is for maximum player count.")]
            public string Title = "Current Online Players ({0}/{1})";

            [YamlMember(Order = 2, Description = "Display the player list in embed fields instead of a basic list.")]
            public bool PlayersFields = true;

            [YamlMember(Order = 3, Description = "If more than one player is online, display the fields side-by-side.")]
            public bool PlayersFieldsInline = true;

            [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field title.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersFieldsTitle = "{0}";

            [YamlMember(Order = 5, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field value.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersFieldsValue = "";

            [YamlMember(Order = 6, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field title with ping.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersFieldsTitleWithPing = "{0}";

            [YamlMember(Order = 7, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field value with ping.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersFieldsValueWithPing = "({1:0}ms)";

            [YamlMember(Order = 8, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player list.\n\"{0}\" holds one of the player list entries below.")]
            public string PlayersList = "{0}\n";

            [YamlMember(Order = 9, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the list.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersListEntry = "{0}\n";

            [YamlMember(Order = 10, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the list with ping.\n\"{0}\" is for player name and \"{1}\" is for ping.")]
            public string PlayersListEntryWithPing = "{0} ({1:0}ms)\n";

            [YamlMember(Order = 11, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Message to replace the fields/list if there are no players currently online.")]
            public string NoPlayersOnline = "*No players online*\n";

            [YamlMember(Order = 12, Description = "Show the formatted UTC time and date in the embed footer.")]
            public bool Timestamp = true;

            [YamlMember(Order = 13, Description = "An \"Ephemeral Message\" is a message sent by Clyde and other Discord bots. It's a message that only you can see. These messages disappear when you dismiss them, wait long enough, or restart Discord.")]
            public bool Ephemeral = true;
        }
    }

    public static BotConfig Reload(Bot bot) {
        BotConfig config = Read();
        CheckForOldConfig(bot, config);
        return Write(config);
    }

    private static BotConfig Read() {
        string? yaml = File.Exists(FILENAME) ? File.ReadAllText(FILENAME) : null;
        if (yaml is not { Length: > 0 }) {
            return new BotConfig();
        }

        try {
            return new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithTypeConverter(new HexTypeConverter())
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build().Deserialize<BotConfig>(yaml);
        }
        catch (Exception) {
            return new BotConfig();
        }
    }

    private static BotConfig Write(BotConfig config) {
        string yaml = new SerializerBuilder()
            .WithQuotingNecessaryStrings()
            .WithTypeConverter(new HexTypeConverter())
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build().Serialize(config);
        File.WriteAllText(FILENAME, yaml, Encoding.UTF8);
        return config;
    }

    private static void CheckForOldConfig(Bot bot, BotConfig curConfig) {
        string fullPath = Path.Combine(GamePaths.ModConfig, OldConfig.Filename);

        if (!File.Exists(fullPath)) {
            return;
        }

        OldConfig oldConfig = bot.Api.LoadModConfig<OldConfig>(OldConfig.Filename);

        File.Delete(fullPath);

        if (oldConfig == null) {
            return;
        }

        curConfig.Token = oldConfig.Token;
        curConfig.ChatChannel = oldConfig.ChatChannel;
        curConfig.ConsoleChannel = oldConfig.ConsoleChannel;

        curConfig.Messages.ServerStarted = oldConfig.Messages.ServerStarted;
        curConfig.Messages.ServerStopped = oldConfig.Messages.ServerStopped;
        curConfig.Messages.BotPresence = oldConfig.Messages.BotPresence;
        curConfig.Messages.PlayerJoined = oldConfig.Messages.PlayerJoined;
        curConfig.Messages.PlayerLeft = oldConfig.Messages.PlayerLeft;
        curConfig.Messages.PlayerDeath = oldConfig.Messages.PlayerDeath;
        curConfig.Messages.PlayerChat = oldConfig.Messages.PlayerChat;
        curConfig.Messages.PlayerChangedCharacter = oldConfig.Messages.PlayerChangedCharacter;
        curConfig.Messages.TemporalStorm = oldConfig.Messages.TemporalStorm;
    }
}
