using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace DiscordBot.Extensions;

public static class PlayerExtensions {
    public static string GetCharacterClass(this EntityPlayer player) {
        return Lang.Get($"characterclass-{player.WatchedAttributes.GetString("characterClass")}");
    }

    public static string GetAvatar(this EntityPlayer player) {
        ITreeAttribute appliedParts = (ITreeAttribute)player.WatchedAttributes.GetTreeAttribute("skinConfig")["appliedParts"];
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
