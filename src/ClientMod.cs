using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace DiscordBot;

public class ClientMod {
    private static readonly Dictionary<SkinnablePart, SkinnablePartVariant> PARTS = new();
    private static GuiDialogCreateCharacter? dialog;
    private static bool findNextSkin;
    private static bool takeScreenshot;

    private readonly ICoreClientAPI capi;
    private readonly ClientPlatformAbstract platform;

    private SKBitmap bitmap, rotated, cropped, scaled;

    public ClientMod(ICoreClientAPI api) {
        capi = api;

        platform = (ClientPlatformAbstract)typeof(ClientMain)
            .GetField("Platform", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue((ClientMain)capi.World)!;

        ClientEventManager eventManager = (ClientEventManager)typeof(ClientMain)
            .GetField("eventManager", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue((ClientMain)capi.World)!;

        eventManager.RegisterRenderer(_ => {
            if (takeScreenshot) {
                Screenshot();
            }
        }, EnumRenderStage.Done, "uh...", 2.0);
    }

    public void Start() {
        dialog = new GuiDialogCreateCharacter(capi, capi.ModLoader.GetModSystem<CharacterSystem>());
        dialog.PrepAndOpen();
        if (!dialog.IsOpened()) {
            dialog.TryOpen();
        }

        ThreadPool.QueueUserWorkItem(_ => {
            Mustache(capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>());
            capi.Event.EnqueueMainThreadTask(() => { dialog?.TryClose(); }, "try close");
        });
    }

    private void BaseSkin(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart baseskin = ebes.AvailableSkinPartsByCode.Get("baseskin")!;
        foreach (SkinnablePartVariant? variant in baseskin.Variants) {
            PARTS.Clear();
            PARTS.Add(baseskin, variant);
            //EyeColor(ebes);
            End(ebes);
        }
    }

    private void EyeColor(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart eyecolor = ebes.AvailableSkinPartsByCode.Get("eyecolor")!;
        foreach (SkinnablePartVariant? variant in eyecolor.Variants) {
            PARTS.Remove(eyecolor);
            PARTS.Add(eyecolor, variant);
            //HairBase(ebes);
            End(ebes);
        }
    }

    private void HairBase(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart hairbase = ebes.AvailableSkinPartsByCode.Get("hairbase")!;
        foreach (SkinnablePartVariant? variant in hairbase.Variants) {
            PARTS.Remove(hairbase);
            PARTS.Add(hairbase, variant);
            //HairExtra(ebes);
            HairColor(ebes);
        }
    }

    private void HairExtra(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart hairextra = ebes.AvailableSkinPartsByCode.Get("hairextra")!;
        foreach (SkinnablePartVariant? variant in hairextra.Variants) {
            PARTS.Remove(hairextra);
            PARTS.Add(hairextra, variant);
            //Expression(ebes);
            HairColor(ebes);
        }
    }

    private void Expression(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart expression = ebes.AvailableSkinPartsByCode.Get("facialexpression")!;
        foreach (SkinnablePartVariant? variant in expression.Variants) {
            PARTS.Remove(expression);
            PARTS.Add(expression, variant);
            //Mustache(ebes);
            End(ebes);
        }
    }

    private void Mustache(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart mustache = ebes.AvailableSkinPartsByCode.Get("mustache")!;
        foreach (SkinnablePartVariant? variant in mustache.Variants) {
            PARTS.Remove(mustache);
            PARTS.Add(mustache, variant);
            //Beard(ebes);
            HairColor(ebes);
        }
    }

    private void Beard(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart beard = ebes.AvailableSkinPartsByCode.Get("beard")!;
        foreach (SkinnablePartVariant? variant in beard.Variants) {
            PARTS.Remove(beard);
            PARTS.Add(beard, variant);
            HairColor(ebes);
        }
    }

    private void HairColor(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart haircolor = ebes.AvailableSkinPartsByCode.Get("haircolor")!;
        foreach (SkinnablePartVariant? variant in haircolor.Variants) {
            PARTS.Remove(haircolor);
            PARTS.Add(haircolor, variant);

            End(ebes);
        }
    }

    private void End(EntityBehaviorExtraSkinnable ebes) {
        capi.Event.EnqueueMainThreadTask(() => { FinalResult(ebes); }, "final result");

        findNextSkin = false;
        while (!findNextSkin) {
            Thread.Sleep(1);
        }
    }

    private void PickPart(EntityBehaviorExtraSkinnable ebes, SkinnablePart skinpart, SkinnablePartVariant variant) {
        ebes.selectSkinPart(skinpart.Code, variant.Code);
        int index = Array.IndexOf(skinpart.Variants, variant);
        if (skinpart is { Type: EnumSkinnableType.Texture, UseDropDown: false }) {
            dialog?.Composers["createcharacter"].ColorListPickerSetValue("picker-" + skinpart.Code, index);
        }
        else {
            dialog?.Composers["createcharacter"].GetDropDown("dropdown-" + skinpart.Code).SetSelectedIndex(index);
        }
    }

    private void FinalResult(EntityBehaviorExtraSkinnable ebes) {
        EntitySkinnableShapeRenderer essr = (capi.World.Player.Entity.Properties.Client.Renderer as EntitySkinnableShapeRenderer)!;
        essr.doReloadShapeAndSkin = false;
        foreach (var entry in PARTS) {
            PickPart(ebes, entry.Key, entry.Value);
        }

        essr.doReloadShapeAndSkin = true;
        essr.TesselateShape();

        capi.Event.RegisterCallback(_ => takeScreenshot = true, 1);
    }

    private void Screenshot() {
        takeScreenshot = false;

        platform.LoadFrameBuffer(EnumFrameBuffer.Default);

        bitmap = new SKBitmap(platform.WindowSize.Width, platform.WindowSize.Height, true);
        GL.ReadPixels(0, 0, platform.WindowSize.Width, platform.WindowSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap.GetPixels());

        rotated = new SKBitmap(bitmap.Width, bitmap.Height, bitmap.ColorType, SKAlphaType.Opaque);
        using (SKCanvas surface = new(rotated)) {
            surface.Translate(bitmap.Width, bitmap.Height);
            surface.RotateDegrees(180);
            surface.Scale(-1, 1, bitmap.Width / 2F, 0);
            surface.DrawBitmap(bitmap, 0, 0);
        }

        cropped = new SKBitmap(256, 256);
        rotated.ExtractSubset(cropped, new SKRectI(835, 301, 1091, 557));

        //scaled = new SKBitmap(64, 64);
        //cropped.ScalePixels(scaled, SKFilterQuality.High);

        string path = Path.Combine(GamePaths.Screenshots, "mustache");
        var arr = PARTS.Values.ToArray();
        for (int i = 0; i < arr.Length - 1; i++) {
            path = Path.Combine(path, arr[i].Code);
        }
        
        //string finalPath = PARTS.Values.Aggregate(Path.Combine(GamePaths.Screenshots, "hairbase"), (current, variant) => Path.Combine(current, Path.Combine(variant.Code))) + ".png";
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        using (Stream stream = File.OpenWrite(Path.Combine(path, $"{arr[^1].Code}.png"))) {
            cropped.Encode(SKEncodedImageFormat.Png, 0).SaveTo(stream);
        }

        findNextSkin = true;
    }
}
