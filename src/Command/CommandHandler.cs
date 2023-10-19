using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Util;

namespace DiscordBot.Command;

public class CommandHandler {
    private readonly Dictionary<string, Command> commands = new();

    private Bot Bot { get; }

    public CommandHandler(Bot bot) {
        Bot = bot;

        Register(new PlayersCommand());
    }

    private void Register(Command command) {
        commands.Add(command.Name.ToLower(), command);
    }

    public void Register(DiscordSocketClient client) {
        foreach (Command command in commands.Values) {
            SlashCommandBuilder builder = new SlashCommandBuilder().WithName(command.Name)
                .WithDescription(command.Description);

            foreach (Command.Option option in command.Options) {
                builder.AddOption(option.Name, option.Type, option.Description, option.IsRequired);
            }

            client.CreateGlobalApplicationCommandAsync(builder.Build());
        }
    }

    public async Task HandleSlashCommands(SocketSlashCommand command) {
        try {
            await commands!.Get(command.Data.Name.ToLower())!.HandleCommand(Bot, command);
        }
        catch (Exception e) {
            Bot.Logger.Error(e);
            await command.RespondAsync(embed: new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription("There was an error running that command!\n\nSee server console for more information.")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build(),
                ephemeral: true
            );
        }
    }
}
