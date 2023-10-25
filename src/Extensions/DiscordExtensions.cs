using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.WebSocket;

namespace DiscordBot.Extensions;

[SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public static class DiscordExtensions {
    private static readonly Regex USER_NAME = new("<@!?([0-9]+)>");
    private static readonly Regex ROLE_NAME = new("<@&([0-9]+)>");
    private static readonly Regex CHANNEL_NAME = new("<#([0-9]+)>");
    private static readonly Regex EMOJI_TAG = new(@"<:(.+):\d+>");
    private static readonly Regex ANCHOR_TAG = new("""<a(?:.*) href="((?:(\w+)(?::\/\/)+)?[\w:\/\\\+-.?=]+)"(?:.*)>([^<]+)<\/a>""");
    private static readonly Regex ANCHOR_MARKDOWN = new(@"\[(.+)\]\(http(?:s)*:\/\/(.+)\)");
    private static readonly Regex PLAIN_URL = new(@"(?<![""(])(?:<?)(http(?:s)*:\/\/(?:\w(?:[\w.\/?=#-]+)+))(?:>?)");

    public static string GetAuthor(this SocketMessage message) {
        return message.Author is SocketGuildUser guildUser ? guildUser.DisplayName : message.Author.GlobalName ?? message.Author.Username;
    }

    public static bool ShouldIgnore(this DiscordSocketClient client, SocketMessage message) {
        return client.CurrentUser.Id == message.Author.Id || message.Author.IsBot || message.Author.IsWebhook || message.Content == "";
    }

    public static T? Get<T>(this IEnumerable<SocketSlashCommandDataOption> collection, string name) {
        return (T?)(from option in collection where option.Name.Equals(name) select option.Value).FirstOrDefault();
    }

    public static SocketApplicationCommand? Get(this IEnumerable<SocketApplicationCommand> collection, Command.Command command) {
        return collection.FirstOrDefault(applicationCommand => applicationCommand.Name == command.Name);
    }

    public static string ParseForDiscord(this string message) {
        string result = message.Replace("&lt;", "<").Replace("&gt;", ">");
        
        foreach (Match match in ANCHOR_TAG.Matches(result)) {
            string url = match.Groups[1].Value;
            string protocol = match.Groups[2].Value;
            string text = match.Groups[3].Value;

            result = result.Replace(match.Value, protocol is "http" or "https" ? $"[{text}]({url})" : text);
        }

        // discord wont parse anchor markdown if [text] is a url
        /*foreach (Match match in PLAIN_URL.Matches(message)) {
            string url = match.Groups[1].Value;
            message = message.Replace(match.Value, $"[{url}]({url})");
        }*/

        return result;
    }
    
    public static string ParseForGame(this string message) {
        string result = message.Replace("<", "&lt;").Replace(">", "&gt;");

        foreach (Match match in ANCHOR_MARKDOWN.Matches(result)) {
            string text = match.Groups[1].Value;
            string url = match.Groups[2].Value;
            result = result.Replace(match.Value, $"""<a href="{url}">{text}</a>""");
        }

        foreach (Match match in PLAIN_URL.Matches(result)) {
            string url = match.Groups[1].Value;
            result = result.Replace(match.Value, $"""<a href="{url}">{url}</a>""");
        }

        foreach (Match match in EMOJI_TAG.Matches(result)) {
            result = result.Replace(match.Value, $":{match.Groups[1].Value}:");
        }

        return EmojiOne.EmojiOne.ToShort(result);
    }

    public static string SanitizeMessage(this DiscordSocketClient client, SocketMessage socketMessage) {
        string message = socketMessage.Content;
        ulong? guildId = (socketMessage.Channel as SocketTextChannel)?.Guild.Id;
        
        foreach (Match match in USER_NAME.Matches(message)) {
            foreach (SocketUser mUser in socketMessage.MentionedUsers) {
                if (mUser.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                string name = mUser switch {
                    SocketGuildUser mGuildUser => mGuildUser.DisplayName,
                    SocketUnknownUser => client.Rest.GetGuildUserAsync(guildId ?? 0, mUser.Id).GetAwaiter().GetResult().DisplayName,
                    _ => "Unknown"
                };

                message = message.Replace(match.Value, $"@{name}");
                break;
            }
        }

        foreach (Match match in ROLE_NAME.Matches(message)) {
            foreach (SocketRole mRole in socketMessage.MentionedRoles) {
                if (mRole.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                message = message.Replace(match.Value, $"@{mRole.Name}");
                break;
            }
        }

        foreach (Match match in CHANNEL_NAME.Matches(message)) {
            foreach (SocketChannel channel in socketMessage.MentionedChannels) {
                if (channel.Id.ToString() != match.Groups[1].Value) {
                    continue;
                }

                message = message.Replace(match.Value, $"#{channel}");
                break;
            }
        }

        return message.ParseForGame();
    }
}
