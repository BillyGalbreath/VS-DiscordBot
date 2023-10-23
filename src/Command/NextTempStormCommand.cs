using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.GameContent;

namespace DiscordBot.Command;

public class NextTempStormCommand : Command {
    private readonly ConfigNextTempStormCommand config;

    public NextTempStormCommand(Bot bot) : base(bot, "nexttempstorm") {
        config = bot.Config.Commands.NextTempStorm;
    }

    public override bool IsEnabled() {
        return config.Enabled;
    }

    public override string GetHelp() {
        return config.Help;
    }

    public override Task HandleCommand(SocketSlashCommand command) {
        EmbedBuilder embed = new();

        TemporalStormRunTimeData data = Bot.Api.ModLoader.GetModSystem<SystemTemporalStability>().StormData;

        if (data.nowStormActive) {
            double days = data.stormActiveTotalDays - Bot.Api.World.Calendar.TotalDays;
            double hours = days * 24f;
            double minutes = (hours - (int)hours) * 60f;

            if (config.TitleActive is { Length: > 0 }) {
                embed
                    .WithColor(config.Color)
                    .WithTitle(config.TitleActive.Format(data.nextStormStrength, (int)hours, (int)minutes));
            }

            if (config.DescriptionActive is { Length: > 0 }) {
                embed
                    .WithColor(config.Color)
                    .WithDescription(config.DescriptionActive.Format(data.nextStormStrength, (int)hours, (int)minutes));
            }
        }
        else {
            double days = data.nextStormTotalDays - Bot.Api.World.Calendar.TotalDays;
            double hours = (days - (int)days) * 24f;
            double minutes = (hours - (int)hours) * 60f;

            if (config.Title is { Length: > 0 }) {
                embed
                    .WithColor(config.ColorActive)
                    .WithTitle(config.Title.Format((int)days, (int)hours, (int)minutes));
            }

            if (config.Description is { Length: > 0 }) {
                embed
                    .WithColor(config.ColorActive)
                    .WithDescription(config.Description.Format((int)days, (int)hours, (int)minutes));
            }
        }

        return command.RespondAsync(embed: embed.Build(), ephemeral: config.Ephemeral);
    }
}
