using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.GameContent;

namespace DiscordBot.Command;

public class NextTempStormCommand : Command {
    private readonly ConfigNextTempStormCommand _config;

    public NextTempStormCommand(Bot bot) : base(bot, "nexttempstorm") {
        _config = bot.Config.Commands.NextTempStorm;
    }

    public override bool IsEnabled() {
        return _config.Enabled;
    }

    public override string GetHelp() {
        return _config.Help;
    }

    public override Task HandleCommand(SocketSlashCommand command) {
        EmbedBuilder embed = new();

        TemporalStormRunTimeData data = Bot.Api.ModLoader.GetModSystem<SystemTemporalStability>().StormData;

        if (data.nowStormActive) {
            double days = data.stormActiveTotalDays - Bot.Api.World.Calendar.TotalDays;
            double hours = days * 24f;
            double minutes = (hours - (int)hours) * 60f;

            if (_config.TitleActive is { Length: > 0 }) {
                embed
                    .WithColor(_config.Color)
                    .WithTitle(_config.TitleActive.Format(data.nextStormStrength, (int)hours, (int)minutes));
            }

            if (_config.DescriptionActive is { Length: > 0 }) {
                embed
                    .WithColor(_config.Color)
                    .WithDescription(_config.DescriptionActive.Format(data.nextStormStrength, (int)hours, (int)minutes));
            }
        } else {
            double days = data.nextStormTotalDays - Bot.Api.World.Calendar.TotalDays;
            double hours = (days - (int)days) * 24f;
            double minutes = (hours - (int)hours) * 60f;

            if (_config.Title is { Length: > 0 }) {
                embed
                    .WithColor(_config.ColorActive)
                    .WithTitle(_config.Title.Format((int)days, (int)hours, (int)minutes));
            }

            if (_config.Description is { Length: > 0 }) {
                embed
                    .WithColor(_config.ColorActive)
                    .WithDescription(_config.Description.Format((int)days, (int)hours, (int)minutes));
            }
        }

        return command.RespondAsync(embed: embed.Build(), ephemeral: _config.Ephemeral);
    }
}
