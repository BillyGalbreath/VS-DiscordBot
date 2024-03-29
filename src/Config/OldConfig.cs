﻿using System.Diagnostics.CodeAnalysis;

namespace DiscordBot.Config;

[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public class OldConfig {
    public const string Filename = "discordbot.json";

    public string Token = "your-bot-token";
    public ulong ChatChannel = 0;
    public ulong ConsoleChannel = 0;

    public ConfigMessages Messages = new();

    public class ConfigMessages {
        public string ServerStarted = ":white_check_mark: **Server has started**";
        public string ServerStopped = ":octagonal_sign: **Server has stopped**";
        public string BotPresence = "with {0:player;players}";
        public string PlayerJoined = "**{0}**\n\n\nClass: _{1}_";
        public string PlayerLeft = "**{0}**";
        public string PlayerDeath = "**{0}**";
        public string PlayerChat = "<strong>[{0}]:</strong> {1}";
        public string PlayerChangedCharacter = "**{0} updated their character**\n\n\nClass: _{1}_";
        public string TemporalStorm = "**:thunder_cloud_rain: {0}**";
    }
}
