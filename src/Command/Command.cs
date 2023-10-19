using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Command;

public abstract class Command {
    public readonly string Name;
    public readonly string Description;

    public readonly List<Option> Options = new();

    protected Command(string name, string description) {
        Name = name;
        Description = description;
    }

    public abstract Task HandleCommand(Bot bot, SocketSlashCommand command);

    public class Option {
        public required string Name;
        public required ApplicationCommandOptionType Type;
        public required string Description;
        public bool IsRequired = false;
    }
}
