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
    private readonly ConfigPlayersCommand _config;

    public PlayersCommand(Bot bot) : base(bot, "players") {
        _config = bot.Config.Commands.Players;

        Options.Add(new Option {
            Name = "ping",
            Type = ApplicationCommandOptionType.Boolean,
            Description = bot.Config.Commands.Players.HelpPing,
            IsRequired = false
        });
    }

    public override bool IsEnabled() {
        return _config.Enabled;
    }

    public override string GetHelp() {
        return _config.Help;
    }

    public override async Task HandleCommand(SocketSlashCommand command) {
        bool ping = command.Data.Options.Get<bool?>("ping") ?? false;
        var list = (IServerPlayer[])Bot.Api.World.AllOnlinePlayers;

        EmbedBuilder? embed = new EmbedBuilder()
            .WithColor(_config.Color);

        if (_config.Title is { Length: > 0 }) {
            embed.WithTitle(_config.Title.Format(list.Length, Bot.Api.Server.Config.MaxClients));
        }

        if (_config.PlayersFields && list.Length > 0) {
            embed.WithFields(from player in list
                let milli = TimeSpan.FromSeconds(player.Ping).Milliseconds
                let title = (ping ? _config.PlayersFieldsTitleWithPing : _config.PlayersFieldsTitle).Format(player.PlayerName, milli)
                let value = (ping ? _config.PlayersFieldsValueWithPing : _config.PlayersFieldsValue).Format(player.PlayerName, milli)
                select new EmbedFieldBuilder()
                    .WithName(title is { Length: > 0 } ? title : "\u200B")
                    .WithValue(value is { Length: > 0 } ? value : "\u200B")
                    .WithIsInline(_config.PlayersFieldsInline)
            );
        } else {
            embed.WithDescription(_config.PlayersList.Format(list.Length > 0
                ? list.Aggregate("", (current, player) => {
                    string format = ping ? _config.PlayersListEntryWithPing : _config.PlayersListEntry;
                    string name = format.Format(player.PlayerName, TimeSpan.FromSeconds(player.Ping).Milliseconds);
                    return current + name;
                })
                : _config.NoPlayersOnline)
            );
        }

        if (_config.Timestamp) {
            embed.WithCurrentTimestamp();
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: _config.Ephemeral);
    }
}
