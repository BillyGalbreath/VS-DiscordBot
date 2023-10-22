using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.API.Server;

namespace DiscordBot.Command;

public class PlayersCommand : Command {
    public PlayersCommand() : base("players", "List current online players") {
        Options.Add(new Option {
            Name = "ping",
            Type = ApplicationCommandOptionType.Boolean,
            Description = "Show players' ping",
            IsRequired = false
        });
    }

    public override async Task HandleCommand(Bot bot, SocketSlashCommand command) {
        BotConfig.ConfigCommands.ConfigPlayersCommand config = bot.Config.Commands.ConfigPlayers;

        bool ping = command.Data.Options.Get<bool?>("ping") ?? false;
        var list = (IServerPlayer[])bot.Api.World.AllOnlinePlayers;

        EmbedBuilder? embed = new EmbedBuilder()
            .WithColor(config.Color);

        if (config.Title is { Length: > 0 }) {
            embed.WithTitle(string.Format(config.Title, list.Length, bot.Api.Server.Config.MaxClients));
        }

        if (config.PlayersFields && list.Length > 0) {
            embed.WithFields(from player in list
                let milli = TimeSpan.FromSeconds(player.Ping).Milliseconds
                let title = string.Format(ping ? config.PlayersFieldsTitleWithPing : config.PlayersFieldsTitle, player.PlayerName, milli)
                let value = string.Format(ping ? config.PlayersFieldsValueWithPing : config.PlayersFieldsValue, player.PlayerName, milli)
                select new EmbedFieldBuilder()
                    .WithName(title is { Length: > 0 } ? title : "\u200B")
                    .WithValue(value is { Length: > 0 } ? value : "\u200B")
                    .WithIsInline(config.PlayersFieldsInline)
            );
        }
        else {
            embed.WithDescription(string.Format(config.PlayersList, list.Length > 0
                ? list.Aggregate("", (current, player) => {
                    string format = ping ? config.PlayersListEntryWithPing : config.PlayersListEntry;
                    string name = string.Format(format, player.PlayerName, TimeSpan.FromSeconds(player.Ping).Milliseconds);
                    return current + name;
                })
                : config.NoPlayersOnline)
            );
        }

        if (config.Timestamp) {
            embed.WithCurrentTimestamp();
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: config.Ephemeral);
    }
}
