using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extensions;
using Vintagestory.API.Util;

namespace DiscordBot.Command;

public class CommandHandler {
    private readonly Dictionary<string, Command> commands = new();

    private Bot Bot { get; }

    public CommandHandler(Bot bot) {
        Bot = bot;

        Register(new NextTempStormCommand(bot));
        Register(new PlayersCommand(bot));
        Register(new TimeCommand(bot));
    }

    private void Register(Command command) {
        commands.Add(command.Name.ToLower(), command);
    }

    public void Register(SocketGuild guild) {
        IReadOnlyCollection<SocketApplicationCommand> registeredCommands = guild.GetApplicationCommandsAsync().Result;

        foreach (Command command in commands.Values) {
            if (registeredCommands.Contains(command)) {
                continue;
            }

            SlashCommandBuilder builder = new SlashCommandBuilder()
                .WithName(command.Name)
                .WithDescription(command.Description);

            foreach (Command.Option option in command.Options) {
                builder.AddOption(option.Name, option.Type, option.Description, option.IsRequired);
            }

            guild.CreateApplicationCommandAsync(builder.Build());
        }
    }

    public async Task HandleSlashCommands(SocketSlashCommand command) {
        try {
            await commands!.Get(command.Data.Name.ToLower())!.HandleCommand(Bot, command);
        }
        catch (Exception e) {
            Bot.Logger.Error(e);
            await command.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle(Bot.Config.Commands.Error.Title)
                    .WithDescription(Bot.Config.Commands.Error.Description)
                    .WithColor(Bot.Config.Commands.Error.Color)
                    .WithCurrentTimestamp()
                    .Build(),
                ephemeral: true
            );
        }
    }
}
