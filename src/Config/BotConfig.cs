using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Discord;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordBot.Config;

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class BotConfig {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Your bot's secret token. Go to https://discord.com/developers/applications to set up your bot.\nAt a minimum bot requires all three privileged intents (https://discord.com/developers/docs/topics/gateway#privileged-intents),\nand bot permissions to manage webhooks (https://discord.com/developers/docs/getting-started#adding-scopes-and-bot-permissions)")]
    public string Token { get; private set; } = "your-bot-token";

    [YamlMember(Order = 1, Description = "Channel ID where you want player chat and events to go to/from.")]
    public ulong ChatChannel { get; private set; }

    [YamlMember(Order = 2, Description = "Channel ID where you want console to log to.\nWARNING: Protect this channel, as any messages sent to it will be run\nas commands directly in the console!\nLeave as 0 if you dont want this feature enabled.")]
    public ulong ConsoleChannel { get; private set; }

    [YamlMember(Order = 3, Description = "Parse urls in the game chat (anchor tags and markdown links become clickable)")]
    public bool ParseUrlsInGameChat { get; private set; } = true;

    [YamlMember(Order = 4, Description = "\nCustomizable messages.")]
    public ConfigMessages Messages = new();

    [YamlMember(Order = 5, Description = "\nDiscord slash command options.")]
    public ConfigCommands Commands = new();

    [YamlMember(Order = int.MaxValue, Description = "\n\nDo not edit this. For internal use only.\n\n(seriously, you can break your bot by editing this)")]
    public int ConfigVersion;

    private static readonly string FILENAME = Path.Combine(GamePaths.ModConfig, "discordbot.yml");

    private const int Version = 1;

    private int previousVersion;

    public static BotConfig Reload() {
        BotConfig config = Read();
        config.Update();
        return Write(config);
    }

    private static BotConfig Read() {
        try {
            return new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithTypeConverter(new HexTypeConverter())
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build().Deserialize<BotConfig>(File.ReadAllText(FILENAME));
        }
        catch (Exception) {
            return new BotConfig();
        }
    }

    private static BotConfig Write(BotConfig config) {
        File.WriteAllText(FILENAME,
            new SerializerBuilder()
                .WithQuotingNecessaryStrings()
                .WithTypeConverter(new HexTypeConverter())
                .WithNamingConvention(NullNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build().Serialize(config)
            , Encoding.UTF8);
        return config;
    }

    private void CheckForOldConfig() {
        string fullPath = Path.Combine(GamePaths.ModConfig, OldConfig.Filename);

        if (!File.Exists(fullPath)) {
            return;
        }

        OldConfig? oldConfig = JsonConvert.DeserializeObject<OldConfig>(File.ReadAllText(fullPath));

        File.Delete(fullPath);

        if (oldConfig == null) {
            return;
        }

        Token = oldConfig.Token;
        ChatChannel = oldConfig.ChatChannel;
        ConsoleChannel = oldConfig.ConsoleChannel;

        Messages.ServerStarted = oldConfig.Messages.ServerStarted;
        Messages.ServerStopped = oldConfig.Messages.ServerStopped;
        Messages.BotPresence = oldConfig.Messages.BotPresence;
        Messages.PlayerJoined = oldConfig.Messages.PlayerJoined;
        Messages.PlayerLeft = oldConfig.Messages.PlayerLeft;
        Messages.PlayerDeath = oldConfig.Messages.PlayerDeath;
        Messages.PlayerChat = oldConfig.Messages.PlayerChat;
        Messages.PlayerChangedCharacter = oldConfig.Messages.PlayerChangedCharacter;
        Messages.TemporalStorm = oldConfig.Messages.TemporalStorm;
    }

    private void Update() {
        previousVersion = ConfigVersion;
        ConfigVersion = Version;

        CheckForOldConfig();

        // badly named entry. renamed in config version 1
        if (previousVersion < 1 && Commands.ConfigPlayers != null) {
            Commands.Players = Commands.ConfigPlayers;
            Commands.ConfigPlayers = null;
        }
    }
}

public class ConfigMessages {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The server is fully started.")]
    public string ServerStarted = ":white_check_mark: **Server has started**";

    [YamlMember(Order = 1, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The server is shutting down.")]
    public string ServerStopped = ":octagonal_sign: **Server has stopped**";

    [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The bot's presence text. (the \"playing with ...\" text in the users list)\n\"{0}\" is for current player count.\n\"{1}\" is for maximum player count.")]
    public string BotPresence = "with {0:player;players}";

    [YamlMember(Order = 3, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has fully joined the server and is now in game.\n\"{0}\" is the player's name.\n\"{1}\" is the player's class.")]
    public string PlayerJoined = "**{0}**\n\n\nClass: _{1}_";

    [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has left the server.\n\"{0}\" is the disconnect message that was sent in-game.\n\"{1}\" is the player's name.")]
    public string PlayerLeft = "**{0}**";

    [YamlMember(Order = 5, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has died.\n\"{0}\" is the death message that was sent in-game.\n\"{1}\" is the player's name.")]
    public string PlayerDeath = "**{0}**";

    [YamlMember(Order = 6, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has chatted in the discord server. This is how it should look in the game.\n\"{0}\" is the player's name.\n\"{1}\" is the message sent.")]
    public string PlayerChat = "<strong>[{0}]:</strong> {1}";

    [YamlMember(Order = 7, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A player has changed their character/class.\n\"{0}\" is the player's name.\n\"{1}\" is the player's class.")]
    public string PlayerChangedCharacter = "**{0} updated their character**\n\n\nClass: _{1}_";

    [YamlMember(Order = 8, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "A temporal storm message was sent in game.\n\"{0}\" is the message that was sent.")]
    public string TemporalStorm = "**:thunder_cloud_rain: {0}**";
}

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public class ConfigCommands {
    [YamlMember(Order = 0, Description = "Options for the error message when a command fails to run.")]
    public CommandError Error = new();

    [YamlMember(Order = 1, Description = "Options for the /nexttempstorm command to show when the amount of days until the next storm.")]
    public ConfigNextTempStormCommand NextTempStorm = new();

    // badly named entry. renamed in config version 1
    public ConfigPlayersCommand? ConfigPlayers;

    [YamlMember(Order = 3, Description = "Options for the /players command to list currently online players.")]
    public ConfigPlayersCommand Players = new();

    [YamlMember(Order = 4, Description = "Options for the /time command to show current in-game time.")]
    public ConfigTimeCommand Time = new();
}

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class CommandError {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed.\nSupports any six-digit color hex value with leading \"#\".")]
    public Color Color = 0x008080;

    [YamlMember(Order = 1, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed.")]
    public string Title = ":octagonal_sign: Error!";

    [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Description text for the embed.")]
    public string Description = "There was an error running that command!\n\nSee server console for more information.";
}

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class ConfigNextTempStormCommand {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The help text describing the command.")]
    public string Help = "Tells you the amount of days until the next storm";

    [YamlMember(Order = 1, Description = "Should the command be enabled and registered?")]
    public bool Enabled = true;

    [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed.\nSupports any six-digit color hex value with leading \"#\".")]
    public Color Color = 0x008080;

    [YamlMember(Order = 3, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed.\n\"{0}\" is number of days.\n\"{1}\" is number of hours.\n\"{2}\" is number of minutes.")]
    public string Title = "Next temporal storm";

    [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Description text for the embed.\n\"{0}\" is number of days.\n\"{1}\" is number of hours.\n\"{2}\" is number of minutes.")]
    public string Description = "is in {0:day;days}, {1:hour;hours}, and {2:minute;minutes}";

    [YamlMember(Order = 5, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed for an active storm.\nSupports any six-digit color hex value with leading \"#\".")]
    public Color ColorActive = 0x008080;

    [YamlMember(Order = 6, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed for an active storm.\n\"{0}\" is current storm strength.\n\"{1}\" is number of hours.\n\"{2}\" is number of minutes.")]
    public string TitleActive = "{0} temporal storm";

    [YamlMember(Order = 7, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Description text for the embed for an active storm.\n\"{0}\" is current storm strength.\n\"{1}\" is number of hours.\n\"{2}\" is number of minutes.")]
    public string DescriptionActive = "is still active for {1:hour;hours}, and {2:minute;minutes}";

    [YamlMember(Order = 8, Description = "An \"Ephemeral Message\" is a message sent by Clyde and other Discord bots.\nIt's a message that only you can see. These messages disappear when\nyou dismiss them, wait long enough, or restart Discord.")]
    public bool Ephemeral = true;
}

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class ConfigPlayersCommand {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The help text describing the command.")]
    public string Help = "List current online players";

    [YamlMember(Order = 1, Description = "Should the command be enabled and registered?")]
    public bool Enabled = true;

    [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The help text describing the command's ping argument.")]
    public string HelpPing = "Show players' ping";

    [YamlMember(Order = 3, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed.\nSupports any six-digit color hex value with leading \"#\".")]
    public Color Color = 0x008080;

    [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed.\n\"{0}\" is for current player count.\n\"{1}\" is for maximum player count.")]
    public string Title = "Current Online Players ({0}/{1})";

    [YamlMember(Order = 5, Description = "Display the player list in embed fields instead of a basic list.")]
    public bool PlayersFields = false;

    [YamlMember(Order = 6, Description = "If more than one player is online, display the fields side-by-side.")]
    public bool PlayersFieldsInline = true;

    [YamlMember(Order = 7, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field title.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersFieldsTitle = "{0}";

    [YamlMember(Order = 8, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field value.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersFieldsValue = "";

    [YamlMember(Order = 9, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field title with ping.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersFieldsTitleWithPing = "{0}";

    [YamlMember(Order = 10, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the field value with ping.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersFieldsValueWithPing = "({1:0}ms)";

    [YamlMember(Order = 11, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player list.\n\"{0}\" holds one of the player list entries below.")]
    public string PlayersList = "{0}\n";

    [YamlMember(Order = 12, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the list.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersListEntry = "{0}\n";

    [YamlMember(Order = 13, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Format for the player entry in the list with ping.\n\"{0}\" is for player name.\n\"{1}\" is for ping.")]
    public string PlayersListEntryWithPing = "{0} ({1:0}ms)\n";

    [YamlMember(Order = 14, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Message to replace the fields/list if there are no players currently online.")]
    public string NoPlayersOnline = "*No players online*\n";

    [YamlMember(Order = 15, Description = "Show the formatted UTC time and date in the embed footer.")]
    public bool Timestamp = true;

    [YamlMember(Order = 16, Description = "An \"Ephemeral Message\" is a message sent by Clyde and other Discord bots.\nIt's a message that only you can see. These messages disappear when\nyou dismiss them, wait long enough, or restart Discord.")]
    public bool Ephemeral = true;
}

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class ConfigTimeCommand {
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The help text describing the command.")]
    public string Help = "Check current in-game date and time";

    [YamlMember(Order = 1, Description = "Should the command be enabled and registered?")]
    public bool Enabled = true;

    [YamlMember(Order = 2, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "The color of the left strip of the embed.\nSupports any six-digit color hex value with leading \"#\".")]
    public Color Color = 0x008080;

    [YamlMember(Order = 3, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Title text for the embed.\n\"{0}\" is the pretty date and time.")]
    public string Title = ":date: Current in-game date and time";

    [YamlMember(Order = 4, ScalarStyle = ScalarStyle.DoubleQuoted, Description = "Description text for the embed.\n\"{0}\" is the day of month.\n\"{1}\" is the month number.\n\"{2}\" is the month name.\n\"{3}\" is the year.\n\"{4}\" is the hours.\n\"{5}\" is the minutes.")]
    public string Description = "{0:0#}. {2}, Year {3}, {4:0#}:{5:0#}";

    [YamlMember(Order = 5, Description = "An \"Ephemeral Message\" is a message sent by Clyde and other Discord bots.\nIt's a message that only you can see. These messages disappear when\nyou dismiss them, wait long enough, or restart Discord.")]
    public bool Ephemeral = true;
}
