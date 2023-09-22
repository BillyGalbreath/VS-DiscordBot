using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace DiscordBot.Util;

public abstract class TemporalStormUtil {
    public static readonly Dictionary<EnumTempStormStrength, TemporalStormText> TEXTS = new() {
        {
            EnumTempStormStrength.Light,
            new TemporalStormText {
                Approaching = Lang.Get("A light temporal storm is approaching", Array.Empty<object>()),
                Imminent = Lang.Get("A light temporal storm is imminent", Array.Empty<object>()),
                Waning = Lang.Get("The temporal storm seems to be waning", Array.Empty<object>())
            }
        }, {
            EnumTempStormStrength.Medium,
            new TemporalStormText {
                Approaching = Lang.Get("A medium temporal storm is approaching", Array.Empty<object>()),
                Imminent = Lang.Get("A medium temporal storm is imminent", Array.Empty<object>()),
                Waning = Lang.Get("The temporal storm seems to be waning", Array.Empty<object>())
            }
        }, {
            EnumTempStormStrength.Heavy,
            new TemporalStormText {
                Approaching = Lang.Get("A heavy temporal storm is approaching", Array.Empty<object>()),
                Imminent = Lang.Get("A heavy temporal storm is imminent", Array.Empty<object>()),
                Waning = Lang.Get("The temporal storm seems to be waning", Array.Empty<object>())
            }
        }
    };

    public class TemporalStormText {
        public required string? Approaching;
        public required string? Imminent;
        public required string? Waning;
    }
}
