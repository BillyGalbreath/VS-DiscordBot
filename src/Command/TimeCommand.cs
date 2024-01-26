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
    private readonly ConfigTimeCommand _config;

    public TimeCommand(Bot bot) : base(bot, "time") {
        _config = bot.Config.Commands.Time;
    }

    public override bool IsEnabled() {
        return _config.Enabled;
    }

    public override string GetHelp() {
        return _config.Help;
    }

    public override Task HandleCommand(SocketSlashCommand command) {
        EmbedBuilder? embed = new EmbedBuilder()
            .WithColor(_config.Color);

        GameCalendar calendar = (GameCalendar)Bot.Api.World.Calendar;

        int day = calendar.DayOfMonth;
        int month = calendar.Month;
        string monthName = Lang.Get("month-" + calendar.MonthName, Array.Empty<object>());
        int year = calendar.Year;
        int hour = (int)calendar.HourOfDay;
        int minute = (int)((calendar.HourOfDay - hour) * 60f);

        if (_config.Title is { Length: > 0 }) {
            embed.WithTitle(_config.Title.Format(day, month, monthName, year, hour, minute));
        }

        if (_config.Description is { Length: > 0 }) {
            embed.WithDescription(_config.Description.Format(day, month, monthName, year, hour, minute));
        }

        return command.RespondAsync(embed: embed.Build(), ephemeral: _config.Ephemeral);
    }
}
