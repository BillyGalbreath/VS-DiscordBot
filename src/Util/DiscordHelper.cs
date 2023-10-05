using Discord.WebSocket;

namespace DiscordBot.Util;

public static class DiscordHelper {
    public static string GetAuthor(this SocketMessage message) {
        return message.Author is SocketGuildUser guildUser ? guildUser.DisplayName : message.Author.GlobalName ?? message.Author.Username;
    }

    public static bool ShouldIgnore(this DiscordSocketClient client, SocketMessage message) {
        return client.CurrentUser.Id == message.Author.Id || message.Author.IsBot || message.Author.IsWebhook || message.Content == "";
    }
}
