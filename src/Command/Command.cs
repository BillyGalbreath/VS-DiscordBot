using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Command;

public abstract class Command {
    protected readonly Bot Bot;
    
    public readonly string Name;
    public readonly List<Option> Options = new();

    protected Command(Bot bot, string name) {
        Bot = bot;
        Name = name;
    }

    public abstract bool IsEnabled();
    
    public abstract string GetHelp();

    public abstract Task HandleCommand(SocketSlashCommand command);

    public class Option {
        public required string Name;
        public required ApplicationCommandOptionType Type;
        public required string Description;
        public bool IsRequired = false;
    }
}
