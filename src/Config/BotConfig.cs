using System.Diagnostics.CodeAnalysis;
using Discord;
using Newtonsoft.Json;

namespace DiscordBot.Config;

[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
public class BotConfig {
    public const string File = "discordbot.json";

    public string Token = "your-bot-token";
    public ulong ChatChannel = 0;
    public ulong ConsoleChannel = 0;

    public ConfigMessages Messages = new();

    public ConfigCommands Commands = new();

    public class ConfigMessages {
        public string PlayerJoined = "**{0}**\n\n\nClass: _{1}_";
        public string PlayerLeft = "**{0}**";
        public string PlayerDeath = "**{0}**";
        public string PlayerChat = "<strong>[{0}]:</strong> {1}";
        public string PlayerChangedCharacter = "**{0} updated their character**\n\n\nClass: _{1}_";
        public string TemporalStorm = "**:thunder_cloud_rain: {0}**";
        public string ServerStarted = ":white_check_mark: **Server has started**";
        public string ServerStopped = ":octagonal_sign: **Server has stopped**";
        public string BotPresence = "with {0:player;players}";
    }

    public class ConfigCommands {
        public Command Players = new();

        public class Command {
            [JsonConverter(typeof(HexConverter))]
            public Color Color = 0x008080;

            public string Title = "Current Online Players ({0}/{1})";
            public bool PlayersFields = true;
            public bool PlayersFieldsInline = true;
            public string PlayersFieldsTitle = "{0}";
            public string PlayersFieldsValue = "";
            public string PlayersFieldsTitleWithPing = "{0}";
            public string PlayersFieldsValueWithPing = "({1:0}ms)";
            public string PlayersList = "{0}\n";
            public string PlayersListEntry = "{0}\n";
            public string PlayersListEntryWithPing = "{0} ({1:0}ms)\n";
            public string NoPlayersOnline = "*No players online*\n";
            public bool Timestamp = true;
            public bool Ephemeral = true;
        }
    }
}
