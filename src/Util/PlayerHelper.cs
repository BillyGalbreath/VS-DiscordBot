using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace DiscordBot.Util;

public static class PlayerHelper {
    public static string GetClass(this IServerPlayer player) {
        return Lang.Get($"characterclass-{player.Entity.WatchedAttributes.GetString("characterClass")}");
    }

    public static string GetAvatar(this IServerPlayer player) {
        ITreeAttribute appliedParts = (ITreeAttribute)player.Entity.WatchedAttributes.GetTreeAttribute("skinConfig")["appliedParts"];
        return $"https://vs.pl3x.net/v1/" +
               $"{appliedParts.GetString("baseskin")}/" +
               $"{appliedParts.GetString("eyecolor")}/" +
               $"{appliedParts.GetString("hairbase")}/" +
               $"{appliedParts.GetString("hairextra")}/" +
               $"{appliedParts.GetString("mustache")}/" +
               $"{appliedParts.GetString("beard")}/" +
               $"{appliedParts.GetString("haircolor")}.png";
    }
}
