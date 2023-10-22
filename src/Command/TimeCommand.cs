using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace DiscordBot.Command;

public class TimeCommand : Command {
    public TimeCommand(Bot bot) : base("time", bot.Config.Commands.Time.Help) { }

    public override Task HandleCommand(Bot bot, SocketSlashCommand command) {
        BotConfig.ConfigCommands.ConfigTimeCommand config = bot.Config.Commands.Time;

        EmbedBuilder? embed = new EmbedBuilder()
            .WithColor(config.Color);

        GameCalendar calendar = (GameCalendar)bot.Api.World.Calendar;

        int day = calendar.DayOfMonth;
        int month = calendar.Month;
        string monthName = Lang.Get("month-" + calendar.MonthName, Array.Empty<object>());
        int year = calendar.Year;
        int hour = (int)calendar.HourOfDay;
        int minute = (int)((calendar.HourOfDay - hour) * 60f);

        if (config.Title is { Length: > 0 }) {
            embed.WithTitle(config.Title.Format(day, month, monthName, year, hour, minute));
        }

        if (config.Description is { Length: > 0 }) {
            embed.WithDescription(config.Description.Format(day, month, monthName, year, hour, minute));
        }

        return command.RespondAsync(embed: embed.Build(), ephemeral: config.Ephemeral);
    }
}
