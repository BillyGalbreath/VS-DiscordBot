using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.GameContent;

namespace DiscordBot.Command;

public class NextTempStormCommand : Command {
    public NextTempStormCommand(Bot bot) : base("nexttempstorm", bot.Config.Commands.NextTempStorm.Help) { }

    public override Task HandleCommand(Bot bot, SocketSlashCommand command) {
        BotConfig.ConfigCommands.ConfigNextTempStormCommand config = bot.Config.Commands.NextTempStorm;

        EmbedBuilder embed = new();

        TemporalStormRunTimeData data = bot.Api.ModLoader.GetModSystem<SystemTemporalStability>().StormData;

        if (data.nowStormActive) {
            double days = data.stormActiveTotalDays - bot.Api.World.Calendar.TotalDays;
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
            double days = data.nextStormTotalDays - bot.Api.World.Calendar.TotalDays;
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
