using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DiscordBot.Patches;

public class CharacterSelectPatch {
    private static readonly List<string> CharacterSelectCache = new();

    public CharacterSelectPatch(Harmony harmony) {
        harmony.Patch(typeof(CharacterSystem).GetMethod("onCharacterSelection", HarmonyPatches.Flags),
            prefix: GetType().GetMethod("OnCharacterSelectionPrefix"),
            postfix: GetType().GetMethod("OnCharacterSelectionPostfix"));
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void OnCharacterSelectionPrefix(IServerPlayer fromPlayer, CharacterSelectionPacket p) {
        // remove from cache to prevent abuse
        CharacterSelectCache.Remove(fromPlayer.PlayerUID);

        bool didSelectBefore = SerializerUtil.Deserialize(fromPlayer.GetModdata("createCharacter"), false);
        if (didSelectBefore && fromPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
            return;
        }

        if (p.DidSelect) {
            CharacterSelectCache.Add(fromPlayer.PlayerUID);
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void OnCharacterSelectionPostfix(IServerPlayer fromPlayer) {
        if (CharacterSelectCache.Remove(fromPlayer.PlayerUID)) {
            DiscordBotMod.Bot?.OnCharacterSelection(fromPlayer);
        }
    }
}
