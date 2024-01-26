using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using DiscordBot.Extensions;
using Vintagestory.API.Util;

namespace DiscordBot.Command;

public class CommandHandler {
    private readonly Dictionary<string, Command> _commands = new();

    private Bot Bot { get; }

    public CommandHandler(Bot bot) {
        Bot = bot;

        Register(new NextTempStormCommand(bot));
        Register(new PlayersCommand(bot));
        Register(new TimeCommand(bot));
    }

    private void Register(Command command) {
        _commands.Add(command.Name.ToLower(), command);
    }

    public async void RegisterAllCommands(SocketGuild guild) {
        IReadOnlyCollection<SocketApplicationCommand> registeredCommands = guild.GetApplicationCommandsAsync().Result;

        foreach (Command command in _commands.Values) {
            SocketApplicationCommand? registeredCommand = registeredCommands.Get(command);
            if (registeredCommand != null) {
                if (!command.IsEnabled()) {
                    await registeredCommand.DeleteAsync();
                }

                continue;
            }

            if (!command.IsEnabled()) {
                continue;
            }

            SlashCommandBuilder builder = new SlashCommandBuilder()
                .WithName(command.Name)
                .WithDescription(command.GetHelp());

            foreach (Command.Option option in command.Options) {
                builder.AddOption(option.Name, option.Type, option.Description, option.IsRequired);
            }

            await guild.CreateApplicationCommandAsync(builder.Build());
        }
    }

    public async Task HandleSlashCommands(SocketSlashCommand command) {
        try {
            Command? registeredCommand = _commands!.Get(command.Data.Name.ToLower());

            if (registeredCommand?.IsEnabled() ?? false) {
                await registeredCommand.HandleCommand(command);
                return;
            }

            await command.DeleteOriginalResponseAsync();
        } catch (Exception e) {
            Bot.Logger.Error(e);
            CommandError error = Bot.Config.Commands.Error;
            await command.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle(error.Title)
                    .WithDescription(error.Description)
                    .WithColor(error.Color)
                    .WithCurrentTimestamp()
                    .Build(),
                ephemeral: true
            );
        }
    }
}
