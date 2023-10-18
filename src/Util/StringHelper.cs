using System.Text.RegularExpressions;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordBot.Util;

public static class StringHelper {
    public static string SanitizeMessage(this DiscordSocketClient? client, SocketMessage message) {
        string msg = message.Content;
        ulong? guildId = (message.Channel as SocketTextChannel)?.Guild.Id;

        foreach (Match match in Regex.Matches(msg, "<@!?([0-9]+)>")) {
            foreach (SocketUser mUser in message.MentionedUsers) {
                if (mUser.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                string name = "Unknown";
                switch (mUser) {
                    case SocketGuildUser mGuildUser:
                        name = mGuildUser.DisplayName;
                        break;
                    case SocketUnknownUser: {
                        RestGuildUser? rgUser = client!.Rest.GetGuildUserAsync(guildId ?? 0, mUser.Id).GetAwaiter().GetResult();
                        name = rgUser.DisplayName;
                        break;
                    }
                }

                msg = Regex.Replace(msg, $"<@!?{match.Groups[1].Value}>", $"@{name}");
                break;
            }
        }

        foreach (Match match in Regex.Matches(msg, "<@&([0-9]+)>")) {
            foreach (SocketRole mRole in message.MentionedRoles) {
                if (mRole.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                msg = msg.Replace($"<@&{match.Groups[1].Value}>", $"@{mRole.Name}");
                break;
            }
        }

        foreach (Match match in Regex.Matches(msg, "<#([0-9]+)>")) {
            foreach (SocketChannel mChannel in message.MentionedChannels) {
                if (mChannel.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                msg = msg.Replace($"<#{match.Groups[1].Value}>", $"#{mChannel}");
                break;
            }
        }

        foreach (Match match in Regex.Matches(msg, "<:(.+):\\d+>")) {
            msg = msg.Replace(match.Value, $":{match.Groups[1].Value}:");
        }

        return EmojiOne.EmojiOne.ToShort(msg);
    }
}
