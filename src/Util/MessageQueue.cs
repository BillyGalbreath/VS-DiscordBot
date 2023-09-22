using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DiscordBot.Util;

public class MessageQueue {
    private readonly ConcurrentQueue<string> queue = new();

    public void Enqueue(string line) {
        queue.Enqueue(line);
    }

    public IEnumerable<string> Process() {
        List<string> result = new();
        string message = "";
        while (queue.TryDequeue(out string? line)) {
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
        queue.Clear();
    }
}
