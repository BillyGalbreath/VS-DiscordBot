using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DiscordBot.Util;

public class MessageQueue {
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly Bot _bot;

    public MessageQueue(Bot bot) {
        _bot = bot;

        bot.Api.Event.RegisterGameTickListener(_ => Process(), 1000);
    }

    public void Enqueue(string line) {
        _queue.Enqueue(line);
    }

    private void Process() {
        ThreadPool.QueueUserWorkItem(_ => {
            foreach (string line in ProcessLine()) {
                string text = line;
                while (text.Length > 0) {
                    if (text.Length > 2000) {
                        _bot.SendMessageToDiscordConsole(text[..2000]);
                        text = text[2000..];
                        continue;
                    }

                    _bot.SendMessageToDiscordConsole(text);
                    break;
                }
            }
        });
    }

    private IEnumerable<string> ProcessLine() {
        List<string> result = new();
        string message = "";
        while (_queue.TryDequeue(out string? line)) {
            if (message.Length + line.Length + 1 > 2000) {
                result.Add(message);
                message = "";
            }

            message += $"{line}\n";
        }

        if (message.Replace("\n", "").Trim().Length > 0) {
            result.Add(message);
        }

        return result.ToArray();
    }

    public void Dispose() {
        _queue.Clear();
    }
}
